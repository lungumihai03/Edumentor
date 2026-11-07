using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using NpgsqlTypes;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);
var cs = builder.Configuration.GetConnectionString("Default")!;
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "";
if (jwtSecret.Length < 32) throw new Exception("JWT_SECRET trebuie să aibă cel puțin 32 caractere.");
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(cs));
builder.Services.AddHttpClient("AI", c => { c.BaseAddress = new Uri(Environment.GetEnvironmentVariable("AI_SERVICE_URL") ?? "http://ai:8000"); c.Timeout = TimeSpan.FromSeconds(120); });
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
 o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer=false, ValidateAudience=false, ValidateLifetime=true, ValidateIssuerSigningKey=true, IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ClockSkew=TimeSpan.FromSeconds(30) };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
app.UseSwagger(); app.UseSwaggerUI();

var uploadDir = Environment.GetEnvironmentVariable("UPLOAD_DIR") ?? "/app/uploads";
Directory.CreateDirectory(uploadDir);
var maxBytes = int.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_MB"), out var mb) ? mb*1024L*1024L : 20L*1024L*1024L;

async Task EnsureSchema(NpgsqlDataSource db) {
 var sql = """
 CREATE TABLE IF NOT EXISTS users(
  id UUID PRIMARY KEY, name TEXT NOT NULL, email TEXT NOT NULL UNIQUE,
  password_hash TEXT NOT NULL, role TEXT NOT NULL CHECK(role IN ('Admin','Teacher','Student')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW());
 CREATE TABLE IF NOT EXISTS cohorts(
  id UUID PRIMARY KEY, teacher_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  name TEXT NOT NULL, description TEXT NOT NULL DEFAULT '', created_at TIMESTAMPTZ NOT NULL DEFAULT NOW());
 CREATE TABLE IF NOT EXISTS cohort_members(
  cohort_id UUID NOT NULL REFERENCES cohorts(id) ON DELETE CASCADE,
  student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), PRIMARY KEY(cohort_id,student_id));
 CREATE TABLE IF NOT EXISTS resources(
  id UUID PRIMARY KEY, teacher_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  cohort_id UUID REFERENCES cohorts(id) ON DELETE SET NULL,
  file_name TEXT NOT NULL, stored_name TEXT NOT NULL, content_type TEXT NOT NULL,
  file_size BIGINT NOT NULL, description TEXT NOT NULL DEFAULT '',
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW());
 ALTER TABLE practices ADD COLUMN IF NOT EXISTS author_id UUID REFERENCES users(id) ON DELETE SET NULL;
 ALTER TABLE practices ADD COLUMN IF NOT EXISTS reviewed_by UUID REFERENCES users(id) ON DELETE SET NULL;
 ALTER TABLE practices ADD COLUMN IF NOT EXISTS reviewed_at TIMESTAMPTZ;
 ALTER TABLE practices ADD COLUMN IF NOT EXISTS review_note TEXT NOT NULL DEFAULT '';
 ALTER TABLE practices ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW();
 """;
 await using var c=db.CreateCommand(sql); await c.ExecuteNonQueryAsync();
}
app.Lifetime.ApplicationStarted.Register(() => {
 using var scope=app.Services.CreateScope();
 var db=scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
 EnsureSchema(db).GetAwaiter().GetResult();
 using var c=db.CreateCommand("INSERT INTO users(id,name,email,password_hash,role) VALUES(@id,'Administrator',@email,@hash,'Admin') ON CONFLICT(email) DO NOTHING");
 c.Parameters.AddWithValue("id",Guid.NewGuid()); c.Parameters.AddWithValue("email",Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@edumentor.local");
 c.Parameters.AddWithValue("hash",BCrypt.Net.BCrypt.HashPassword(Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "ChangeMe123!")); c.ExecuteNonQuery();
});

string MakeToken(Guid id,string name,string email,string role) {
 var claims=new[]{new Claim(ClaimTypes.NameIdentifier,id.ToString()),new Claim(ClaimTypes.Name,name),new Claim(ClaimTypes.Email,email),new Claim(ClaimTypes.Role,role)};
 var creds=new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),SecurityAlgorithms.HmacSha256);
 return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims:claims,expires:DateTime.UtcNow.AddHours(12),signingCredentials:creds));
}
Guid UserId(ClaimsPrincipal u)=>Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
bool IsRole(ClaimsPrincipal u,string role)=>u.IsInRole(role);
app.MapGet("/",()=>Results.Ok(new {name="EduMentor AI Lab API",version="1.0.0"}));
app.MapGet("/api/health",async(NpgsqlDataSource db)=>{await using var c=db.CreateCommand("SELECT 1");await c.ExecuteScalarAsync();return Results.Ok(new{status="healthy",database="connected"});});

app.MapPost("/api/auth/register",async(RegisterDto d,NpgsqlDataSource db)=>{
 if(string.IsNullOrWhiteSpace(d.Name)||string.IsNullOrWhiteSpace(d.Email)||d.Password.Length<8) return Results.BadRequest(new{error="Completează numele, emailul și o parolă de minimum 8 caractere."});
 if(d.Role!="Teacher") return Results.BadRequest(new{error="Înregistrarea publică este disponibilă doar pentru profesori. Elevii sunt adăugați de profesor în cohorte."});
 var id=Guid.NewGuid();
 try { await using var c=db.CreateCommand("INSERT INTO users(id,name,email,password_hash,role) VALUES(@id,@name,@email,@hash,'Teacher')");
 c.Parameters.AddWithValue("id",id);c.Parameters.AddWithValue("name",d.Name.Trim());c.Parameters.AddWithValue("email",d.Email.Trim().ToLower());c.Parameters.AddWithValue("hash",BCrypt.Net.BCrypt.HashPassword(d.Password));await c.ExecuteNonQueryAsync(); }
 catch(PostgresException e) when(e.SqlState=="23505"){return Results.Conflict(new{error="Există deja un cont cu acest email."});}
 return Results.Ok(new{token=MakeToken(id,d.Name,d.Email,"Teacher"),user=new{id,name=d.Name,email=d.Email,role="Teacher"}});
});
app.MapPost("/api/auth/login",async(LoginDto d,NpgsqlDataSource db)=>{
 await using var c=db.CreateCommand("SELECT id,name,email,password_hash,role FROM users WHERE email=@email");c.Parameters.AddWithValue("email",d.Email.Trim().ToLower());
 await using var r=await c.ExecuteReaderAsync();if(!await r.ReadAsync()||!BCrypt.Net.BCrypt.Verify(d.Password,r.GetString(3)))return Results.Unauthorized();
 var id=r.GetGuid(0);var name=r.GetString(1);var email=r.GetString(2);var role=r.GetString(4);
 return Results.Ok(new{token=MakeToken(id,name,email,role),user=new{id,name,email,role}});
});
app.MapGet("/api/auth/me",(ClaimsPrincipal u)=>Results.Ok(new{id=UserId(u),name=u.Identity!.Name,email=u.FindFirstValue(ClaimTypes.Email),role=u.FindFirstValue(ClaimTypes.Role)})).RequireAuthorization();

app.MapGet("/api/practices",async(NpgsqlDataSource db,string? q,string? language)=>{
 await using var c=db.CreateCommand("SELECT p.id,p.title,p.description,p.problem,p.method,p.subject,p.education_level,p.language,p.context,p.author_name,p.status,COALESCE(u.name,p.author_name) FROM practices p LEFT JOIN users u ON u.id=p.author_id WHERE p.status='Published' AND (CAST(@q AS text) IS NULL OR p.title ILIKE '%'||CAST(@q AS text)||'%' OR p.description ILIKE '%'||CAST(@q AS text)||'%' OR p.problem ILIKE '%'||CAST(@q AS text)||'%') AND (CAST(@language AS text) IS NULL OR p.language=CAST(@language AS text)) ORDER BY p.created_at DESC");
 c.Parameters.AddWithValue("q",(object?)q??DBNull.Value);c.Parameters.AddWithValue("language",(object?)language??DBNull.Value);
 await using var r=await c.ExecuteReaderAsync();var rows=new List<object>();while(await r.ReadAsync())rows.Add(new{id=r.GetGuid(0),title=r.GetString(1),description=r.GetString(2),problem=r.GetString(3),method=r.GetString(4),subject=r.GetString(5),educationLevel=r.GetString(6),language=r.GetString(7),context=r.GetString(8),authorName=r.GetString(11),status=r.GetString(10)});return Results.Ok(rows);
});
app.MapPost("/api/practices",async(PracticeDto p,ClaimsPrincipal u,NpgsqlDataSource db)=>{
 if(!IsRole(u,"Teacher")&&!IsRole(u,"Admin"))return Results.Forbid();
 if(string.IsNullOrWhiteSpace(p.Title))return Results.BadRequest(new{error="Titlul este obligatoriu."});
 var id=Guid.NewGuid();await using var c=db.CreateCommand("INSERT INTO practices(id,title,description,problem,method,subject,education_level,language,context,status,author_name,author_id) VALUES(@id,@title,@description,@problem,@method,@subject,@level,@language,@context,'PendingReview',@author,@authorid)");
 c.Parameters.AddWithValue("id",id);c.Parameters.AddWithValue("title",p.Title);c.Parameters.AddWithValue("description",p.Description??"");c.Parameters.AddWithValue("problem",p.Problem??"");c.Parameters.AddWithValue("method",p.Method??"");c.Parameters.AddWithValue("subject",p.Subject??"");c.Parameters.AddWithValue("level",p.EducationLevel??"");c.Parameters.AddWithValue("language",p.Language??"ro");c.Parameters.AddWithValue("context",p.Context??"");c.Parameters.AddWithValue("author",u.Identity!.Name??"Profesor");c.Parameters.AddWithValue("authorid",UserId(u));await c.ExecuteNonQueryAsync();return Results.Created($"/api/practices/{id}",new{id,status="PendingReview",message="Practica a fost trimisă spre moderare."});
}).RequireAuthorization();

app.MapGet("/api/teacher/practices",async(ClaimsPrincipal u,NpgsqlDataSource db)=>{
 await using var c=db.CreateCommand("SELECT id,title,subject,status,created_at,review_note FROM practices WHERE author_id=@id ORDER BY created_at DESC");c.Parameters.AddWithValue("id",UserId(u));await using var r=await c.ExecuteReaderAsync();var a=new List<object>();while(await r.ReadAsync())a.Add(new{id=r.GetGuid(0),title=r.GetString(1),subject=r.GetString(2),status=r.GetString(3),createdAt=r.GetDateTime(4),reviewNote=r.GetString(5)});return Results.Ok(a);
}).RequireAuthorization();

app.MapGet("/api/admin/practices",async(NpgsqlDataSource db)=> {
 await using var c=db.CreateCommand("SELECT p.id,p.title,p.subject,p.status,p.author_name,p.created_at,p.review_note FROM practices p WHERE p.status<>'Published' ORDER BY p.created_at");await using var r=await c.ExecuteReaderAsync();var a=new List<object>();while(await r.ReadAsync())a.Add(new{id=r.GetGuid(0),title=r.GetString(1),subject=r.GetString(2),status=r.GetString(3),authorName=r.GetString(4),createdAt=r.GetDateTime(5),reviewNote=r.GetString(6)});return Results.Ok(a);
}).RequireAuthorization().RequireAuthorization(policy=>policy.RequireRole("Admin"));
app.MapPost("/api/admin/practices/{id:guid}/review",async(Guid id,ReviewDto d,ClaimsPrincipal u,NpgsqlDataSource db)=>{
 if(d.Decision!="Published"&&d.Decision!="Rejected")return Results.BadRequest(new{error="Decizia trebuie să fie Published sau Rejected."});
 await using var c=db.CreateCommand("UPDATE practices SET status=@status,reviewed_by=@reviewer,reviewed_at=NOW(),review_note=@note,updated_at=NOW() WHERE id=@id AND status='PendingReview'");c.Parameters.AddWithValue("status",d.Decision);c.Parameters.AddWithValue("reviewer",UserId(u));c.Parameters.AddWithValue("note",d.Note??"");c.Parameters.AddWithValue("id",id);var n=await c.ExecuteNonQueryAsync();return n==0?Results.NotFound():Results.Ok(new{status=d.Decision});
}).RequireAuthorization().RequireAuthorization(policy=>policy.RequireRole("Admin"));

app.MapGet("/api/cohorts",async(ClaimsPrincipal u,NpgsqlDataSource db)=>{
 var sql=IsRole(u,"Student")?"SELECT c.id,c.name,c.description,u.name,(SELECT count(*) FROM cohort_members m WHERE m.cohort_id=c.id) FROM cohorts c JOIN users u ON u.id=c.teacher_id JOIN cohort_members mine ON mine.cohort_id=c.id WHERE mine.student_id=@id ORDER BY c.created_at DESC":"SELECT c.id,c.name,c.description,u.name,(SELECT count(*) FROM cohort_members m WHERE m.cohort_id=c.id) FROM cohorts c JOIN users u ON u.id=c.teacher_id WHERE c.teacher_id=@id ORDER BY c.created_at DESC";
 await using var c=db.CreateCommand(sql);c.Parameters.AddWithValue("id",UserId(u));await using var r=await c.ExecuteReaderAsync();var a=new List<object>();while(await r.ReadAsync())a.Add(new{id=r.GetGuid(0),name=r.GetString(1),description=r.GetString(2),teacher=r.GetString(3),students=r.GetInt64(4)});return Results.Ok(a);
}).RequireAuthorization();
app.MapPost("/api/cohorts",async(CohortDto d,ClaimsPrincipal u,NpgsqlDataSource db)=>{
 if(!IsRole(u,"Teacher"))return Results.Forbid();var id=Guid.NewGuid();await using var c=db.CreateCommand("INSERT INTO cohorts(id,teacher_id,name,description) VALUES(@id,@teacher,@name,@desc)");c.Parameters.AddWithValue("id",id);c.Parameters.AddWithValue("teacher",UserId(u));c.Parameters.AddWithValue("name",d.Name);c.Parameters.AddWithValue("desc",d.Description??"");await c.ExecuteNonQueryAsync();return Results.Ok(new{id,name=d.Name});
}).RequireAuthorization();
app.MapGet("/api/cohorts/{id:guid}/students",async(Guid id,ClaimsPrincipal u,NpgsqlDataSource db)=>{
 await using var c=db.CreateCommand("SELECT u.id,u.name,u.email FROM cohort_members m JOIN users u ON u.id=m.student_id JOIN cohorts co ON co.id=m.cohort_id WHERE co.id=@cohort AND co.teacher_id=@teacher");c.Parameters.AddWithValue("cohort",id);c.Parameters.AddWithValue("teacher",UserId(u));await using var r=await c.ExecuteReaderAsync();var a=new List<object>();while(await r.ReadAsync())a.Add(new{id=r.GetGuid(0),name=r.GetString(1),email=r.GetString(2)});return Results.Ok(a);
}).RequireAuthorization().RequireAuthorization(policy=>policy.RequireRole("Teacher"));
app.MapPost("/api/cohorts/{id:guid}/students",async(Guid id,AddStudentDto d,ClaimsPrincipal u,NpgsqlDataSource db)=>{
 if(!IsRole(u,"Teacher"))return Results.Forbid();await using var verify=db.CreateCommand("SELECT 1 FROM cohorts WHERE id=@id AND teacher_id=@teacher");verify.Parameters.AddWithValue("id",id);verify.Parameters.AddWithValue("teacher",UserId(u));if(await verify.ExecuteScalarAsync()==null)return Results.NotFound();
 var email=d.Email.Trim().ToLower();Guid studentId;
 await using(var find=db.CreateCommand("SELECT id FROM users WHERE email=@email AND role='Student'")){find.Parameters.AddWithValue("email",email);var found=await find.ExecuteScalarAsync();if(found is null){studentId=Guid.NewGuid();await using var ins=db.CreateCommand("INSERT INTO users(id,name,email,password_hash,role) VALUES(@id,@name,@email,@hash,'Student')");ins.Parameters.AddWithValue("id",studentId);ins.Parameters.AddWithValue("name",d.Name);ins.Parameters.AddWithValue("email",email);ins.Parameters.AddWithValue("hash",BCrypt.Net.BCrypt.HashPassword(d.TemporaryPassword));await ins.ExecuteNonQueryAsync();}else studentId=(Guid)found;}
 await using var link=db.CreateCommand("INSERT INTO cohort_members(cohort_id,student_id) VALUES(@cohort,@student) ON CONFLICT DO NOTHING");link.Parameters.AddWithValue("cohort",id);link.Parameters.AddWithValue("student",studentId);await link.ExecuteNonQueryAsync();return Results.Ok(new{studentId,email,message="Elevul a fost adăugat în cohortă."});
}).RequireAuthorization().RequireAuthorization(policy=>policy.RequireRole("Teacher"));

app.MapGet("/api/resources",async(ClaimsPrincipal u,NpgsqlDataSource db)=>{
 var sql=IsRole(u,"Teacher")?"SELECT id,file_name,description,content_type,file_size,created_at,cohort_id FROM resources WHERE teacher_id=@id ORDER BY created_at DESC":"SELECT DISTINCT r.id,r.file_name,r.description,r.content_type,r.file_size,r.created_at,r.cohort_id FROM resources r JOIN cohort_members m ON m.cohort_id=r.cohort_id WHERE m.student_id=@id ORDER BY r.created_at DESC";
 await using var c=db.CreateCommand(sql);c.Parameters.AddWithValue("id",UserId(u));await using var r=await c.ExecuteReaderAsync();var a=new List<object>();while(await r.ReadAsync())a.Add(new{id=r.GetGuid(0),fileName=r.GetString(1),description=r.GetString(2),contentType=r.GetString(3),fileSize=r.GetInt64(4),createdAt=r.GetDateTime(5),cohortId=r.IsDBNull(6)?(Guid?)null:r.GetGuid(6)});return Results.Ok(a);
}).RequireAuthorization();
app.MapPost("/api/resources",async(HttpRequest req,ClaimsPrincipal u,NpgsqlDataSource db)=>{
 if(!IsRole(u,"Teacher"))return Results.Forbid();if(!req.HasFormContentType)return Results.BadRequest(new{error="Aștept un fișier multipart/form-data."});
 var form=await req.ReadFormAsync();var file=form.Files.GetFile("file");if(file is null||file.Length==0)return Results.BadRequest(new{error="Selectează un fișier."});if(file.Length>maxBytes)return Results.BadRequest(new{error=$"Fișierul depășește limita de {maxBytes/1024/1024} MB."});
 Guid? cohort=null;if(Guid.TryParse(form["cohortId"],out var parsed))cohort=parsed;
 if(cohort.HasValue){await using var check=db.CreateCommand("SELECT 1 FROM cohorts WHERE id=@id AND teacher_id=@teacher");check.Parameters.AddWithValue("id",cohort.Value);check.Parameters.AddWithValue("teacher",UserId(u));if(await check.ExecuteScalarAsync()==null)return Results.Forbid();}
 var id=Guid.NewGuid();var stored=id+Path.GetExtension(Path.GetFileName(file.FileName));var path=Path.Combine(uploadDir,stored);await using(var fs=File.Create(path))await file.CopyToAsync(fs);
 await using var c=db.CreateCommand("INSERT INTO resources(id,teacher_id,cohort_id,file_name,stored_name,content_type,file_size,description) VALUES(@id,@teacher,@cohort,@name,@stored,@type,@size,@desc)");
 c.Parameters.AddWithValue("id",id);c.Parameters.AddWithValue("teacher",UserId(u));c.Parameters.AddWithValue("cohort",(object?)cohort??DBNull.Value);c.Parameters.AddWithValue("name",Path.GetFileName(file.FileName));c.Parameters.AddWithValue("stored",stored);c.Parameters.AddWithValue("type",file.ContentType);c.Parameters.AddWithValue("size",file.Length);c.Parameters.AddWithValue("desc",form["description"].ToString());await c.ExecuteNonQueryAsync();return Results.Ok(new{id,fileName=file.FileName});
}).RequireAuthorization();
app.MapGet("/api/resources/{id:guid}/download",async(Guid id,ClaimsPrincipal u,NpgsqlDataSource db)=>{
 await using var c=db.CreateCommand("SELECT r.stored_name,r.file_name FROM resources r LEFT JOIN cohort_members m ON m.cohort_id=r.cohort_id WHERE r.id=@id AND (r.teacher_id=@user OR m.student_id=@user)");c.Parameters.AddWithValue("id",id);c.Parameters.AddWithValue("user",UserId(u));await using var r=await c.ExecuteReaderAsync();if(!await r.ReadAsync())return Results.NotFound();var stored=r.GetString(0);var name=r.GetString(1);var path=Path.Combine(uploadDir,stored);if(!File.Exists(path))return Results.NotFound();return Results.File(path,"application/octet-stream",name);
}).RequireAuthorization();

app.MapPost("/api/ai/analyze",async(JsonElement input,IHttpClientFactory f,ClaimsPrincipal u)=>{var r=await f.CreateClient("AI").PostAsJsonAsync("/analyze",input);return Results.Content(await r.Content.ReadAsStringAsync(),"application/json",statusCode:(int)r.StatusCode);}).RequireAuthorization();
app.MapPost("/api/ai/adapt",async(JsonElement input,IHttpClientFactory f,ClaimsPrincipal u)=>{var r=await f.CreateClient("AI").PostAsJsonAsync("/adapt",input);return Results.Content(await r.Content.ReadAsStringAsync(),"application/json",statusCode:(int)r.StatusCode);}).RequireAuthorization();

app.Run();

record RegisterDto(string Name,string Email,string Password,string Role);
record LoginDto(string Email,string Password);
record PracticeDto(string Title,string? Description,string? Problem,string? Method,string? Subject,string? EducationLevel,string? Language,string? Context);
record ReviewDto(string Decision,string? Note);
record CohortDto(string Name,string? Description);
record AddStudentDto(string Name,string Email,string TemporaryPassword);
