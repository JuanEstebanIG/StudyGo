using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.Services;
using StudyGo.ViewModels.Tareas;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace StudyGo.Controllers
{
    public class TareasController : Controller
    {
        private readonly IAcademicService _academicService;

        public TareasController(IAcademicService academicService)
        {
            _academicService = academicService;
        }

        private string GetCurrentRole()
        {
            if (User.IsInRole("Administrador")) return "Administrador";
            if (User.IsInRole("Docente")) return "Docente";
            return "Estudiante";
        }

        private Guid GetCurrentUserId()
        {
            return GetCurrentRole() == "Docente" ? AcademicService.DocenteId : AcademicService.Estudiante1Id;
        }

        // GET: /Tareas
        public async Task<IActionResult> Index()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();
            var courses = await _academicService.GetCoursesForUserAsync(userId, role);

            var items = new List<TareaListItemViewModel>();

            foreach (var course in courses)
            {
                var courseDetail = await _academicService.GetCourseDetailAsync(course.Id);
                if (courseDetail == null) continue;

                var tasks = courseDetail.Activities.OfType<ProgrammingTask>().ToList();

                foreach (var task in tasks)
                {
                    if (role == "Estudiante")
                    {
                        // Estudiante: ver su estado de entrega
                        var submission = await _academicService.GetOrCreateSubmissionAsync(task.Id, userId);
                        items.Add(new TareaListItemViewModel
                        {
                            Id = task.Id,
                            Title = task.Title,
                            CourseName = courseDetail.Name,
                            CourseId = courseDetail.Id,
                            Language = task.Language,
                            State = task.State.ToString(),
                            SubmissionStatus = submission?.Status.ToString() ?? "SinEmpezar",
                            Grade = submission?.Grade?.FinalScore
                        });
                    }
                    else
                    {
                        // Docente / Admin: ver estadísticas de entregas
                        var submissions = (await _academicService.GetTaskSubmissionsAsync(task.Id)).ToList();
                        items.Add(new TareaListItemViewModel
                        {
                            Id = task.Id,
                            Title = task.Title,
                            CourseName = courseDetail.Name,
                            CourseId = courseDetail.Id,
                            Language = task.Language,
                            State = task.State.ToString(),
                            TotalSubmissions = submissions.Count,
                            PendingGrading = submissions.Count(s => s.Status.ToString() == "Enviado"),
                            Graded = submissions.Count(s => s.Status.ToString() == "Calificado")
                        });
                    }
                }
            }

            var vm = new TareasIndexViewModel
            {
                Role = role,
                Tareas = items
            };

            return View(vm);
        }

        // GET: /Tareas/Crear
        public async Task<IActionResult> Crear(Guid? courseId = null)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();
            var courses = (await _academicService.GetCoursesForUserAsync(userId, role)).ToList();

            var vm = new TareaCrearEditarViewModel
            {
                CourseId = courseId ?? (courses.FirstOrDefault()?.Id ?? Guid.Empty),
                AvailableCourses = courses.Select(c => (c.Id, c.Name)).ToList()
            };
            return View(vm);
        }

        // POST: /Tareas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(TareaCrearEditarViewModel vm)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                var courses = (await _academicService.GetCoursesForUserAsync(userId, role)).ToList();
                vm.AvailableCourses = courses.Select(c => (c.Id, c.Name)).ToList();
                return View(vm);
            }

            var task = new ProgrammingTask
            {
                CourseId = vm.CourseId,
                Title = vm.Title,
                Description = vm.Description,
                Language = vm.Language,
                TimeLimitSeconds = vm.TimeLimitSeconds,
                MemoryLimitMb = vm.MemoryLimitMb,
                State = StudyGo.Enums.ActivityState.Publicado
            };

            await _academicService.CreateTaskAsync(task);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Tareas/Editar/{id}
        public async Task<IActionResult> Editar(Guid id)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();

            var task = await _academicService.GetTaskDetailAsync(id);
            if (task == null) return NotFound();

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();
            var courses = (await _academicService.GetCoursesForUserAsync(userId, role)).ToList();

            var vm = new TareaCrearEditarViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Language = task.Language,
                TimeLimitSeconds = task.TimeLimitSeconds,
                MemoryLimitMb = task.MemoryLimitMb,
                CourseId = task.CourseId,
                CourseName = task.Course?.Name ?? "",
                AvailableCourses = courses.Select(c => (c.Id, c.Name)).ToList()
            };
            return View(vm);
        }

        // POST: /Tareas/Editar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Guid id, TareaCrearEditarViewModel vm)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();
            if (id != vm.Id) return BadRequest();

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                var courses = (await _academicService.GetCoursesForUserAsync(userId, role)).ToList();
                vm.AvailableCourses = courses.Select(c => (c.Id, c.Name)).ToList();
                return View(vm);
            }

            var task = new ProgrammingTask
            {
                Id = vm.Id,
                CourseId = vm.CourseId,
                Title = vm.Title,
                Description = vm.Description,
                Language = vm.Language,
                TimeLimitSeconds = vm.TimeLimitSeconds,
                MemoryLimitMb = vm.MemoryLimitMb,
                State = StudyGo.Enums.ActivityState.Publicado
            };

            var success = await _academicService.UpdateTaskAsync(task);
            if (!success) return NotFound();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Tareas/Eliminar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();
            await _academicService.DeleteTaskAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detalle(Guid id, Guid? versionId = null)
        {
            var task = await _academicService.GetTaskDetailAsync(id);
            if (task == null) return NotFound();

            var role = GetCurrentRole();
            var course = await _academicService.GetCourseDetailAsync(task.CourseId);

            string currentCode = "using System;\n\nclass Program {\n    static void Main() {\n        Console.WriteLine(\"¡Hola StudyGo!\");\n    }\n}";
            var versionList = new List<VersionItemViewModel>();
            Guid submissionId = Guid.Empty;
            string submissionStatus = "SinEmpezar";
            decimal? finalScore = null;

            if (role == "Estudiante")
            {
                // Solo los estudiantes obtienen/crean su propia submission
                var userId = GetCurrentUserId();
                var submission = await _academicService.GetOrCreateSubmissionAsync(id, userId);

                var versions = await _academicService.GetSubmissionVersionsAsync(submission.Id);
                versionList = versions.Select(v => new VersionItemViewModel
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    SavedAt = v.SavedAt,
                    Status = submission.Status == SubmissionStatus.Calificado || (submission.Status == SubmissionStatus.Enviado && v.VersionNumber == versions.Max(x => x.VersionNumber)) ? "Oficial" : "En progreso"
                }).ToList();

                if (versionId.HasValue)
                {
                    var v = await _academicService.GetSubmissionVersionAsync(versionId.Value);
                    if (v != null) currentCode = v.Code;
                }
                else if (versions.Any())
                {
                    currentCode = versions.First().Code;
                }

                submissionId = submission.Id;
                submissionStatus = submission.Status.ToString();
                finalScore = submission.Grade?.FinalScore;
            }
            else
            {
                // Docente / Administrador: modo vista previa — sin submission propia
                // Pueden cargar una versión específica si se pasa versionId
                if (versionId.HasValue)
                {
                    var v = await _academicService.GetSubmissionVersionAsync(versionId.Value);
                    if (v != null) currentCode = v.Code;
                }
            }

            var vm = new TareaDetalleViewModel
            {
                Id = task.Id,
                CourseId = task.CourseId,
                CourseName = course?.Name ?? "Curso",
                Title = task.Title,
                Description = task.Description,
                Language = task.Language,
                TimeLimitSeconds = task.TimeLimitSeconds,
                MemoryLimitMb = task.MemoryLimitMb,
                RubricTitle = task.Rubric != null ? "Rúbrica de la Tarea" : "Sin Rúbrica",
                Role = role,
                SubmissionId = submissionId,
                SubmissionStatus = submissionStatus,
                CurrentCode = currentCode,
                FinalScore = finalScore,
                Versions = versionList
            };

            return View(vm);
        }

        // GET: /Tareas/ObtenerCodigoVersion?versionId={versionId}
        public async Task<IActionResult> ObtenerCodigoVersion(Guid versionId)
        {
            var v = await _academicService.GetSubmissionVersionAsync(versionId);
            if (v == null) return NotFound();
            return Content(v.Code, "text/plain");
        }

        private async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
            string fileName, string arguments, string? stdin = null, string? workingDir = null, int timeoutMs = 10000)
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = stdin != null;
            process.StartInfo.CreateNoWindow = true;
            if (workingDir != null)
            {
                process.StartInfo.WorkingDirectory = workingDir;
            }

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) stdoutBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (stdin != null)
            {
                using var writer = process.StandardInput;
                await writer.WriteAsync(stdin);
            }

            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                await process.WaitForExitAsync(cts.Token);
                return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
            }
            catch (OperationCanceledException)
            {
                process.Kill(true);
                return (-1, stdoutBuilder.ToString(), "Error: Tiempo de ejecución excedido (10 segundos).");
            }
        }

        // POST: /Tareas/EjecutarCode
        [HttpPost]
        public async Task<IActionResult> EjecutarCode([FromBody] EjecutarCodigoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return Json(new EjecutarCodigoResponse
                {
                    Status = "Error",
                    Stderr = "El código no puede estar vacío.",
                    ExecutionTimeMs = 0
                });
            }

            // Analizar el código para simular diferentes comportamientos interactivos
            if (request.Code.Contains("simulate-pending"))
            {
                return Json(new EjecutarCodigoResponse
                {
                    Status = "Pendiente",
                    Message = "El sandbox de ejecución externo no está disponible actualmente. Tu solicitud ha sido encolada (Circuit Breaker activo)."
                });
            }

            if (request.Code.Contains("simulate-timeout"))
            {
                return Json(new EjecutarCodigoResponse
                {
                    Status = "Tiempo agotado",
                    Stderr = "Execution Timeout: La ejecución superó el límite de 10 segundos configurado en la tarea.",
                    ExecutionTimeMs = 10005
                });
            }

            if (request.Code.Contains("simulate-error"))
            {
                return Json(new EjecutarCodigoResponse
                {
                    Status = "Error",
                    Stderr = "Compilation Error (line 5, col 9): ';' expected\nSystem.Exception: Compilation failed.",
                    ExecutionTimeMs = 120
                });
            }

            var sandboxId = Guid.NewGuid().ToString("N");
            var sandboxDir = Path.Combine(Directory.GetCurrentDirectory(), "obj", "Sandbox", sandboxId);
            Directory.CreateDirectory(sandboxDir);

            try
            {
                string lang = (request.Language ?? "C#").ToLower();
                string executableName = "";
                string runArgs = "";
                bool compileSuccess = true;
                string compileError = "";

                if (lang == "csharp" || lang == "c#")
                {
                    var codePath = Path.Combine(sandboxDir, "Program.cs");
                    await System.IO.File.WriteAllTextAsync(codePath, request.Code);

                    var csprojPath = Path.Combine(sandboxDir, "Sandbox.csproj");
                    var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
                    await System.IO.File.WriteAllTextAsync(csprojPath, csprojContent);

                    // Compile C#
                    var (buildExitCode, buildStdout, buildStderr) = await RunProcessAsync("dotnet", $"build \"{csprojPath}\" -c Release", timeoutMs: 20000);
                    if (buildExitCode != 0)
                    {
                        compileSuccess = false;
                        compileError = buildStdout + "\n" + buildStderr;
                    }
                    else
                    {
                        executableName = "dotnet";
                        runArgs = $"\"{Path.Combine(sandboxDir, "bin", "Release", "net10.0", "Sandbox.dll")}\"";
                    }
                }
                else if (lang == "java")
                {
                    var codePath = Path.Combine(sandboxDir, "Program.java");
                    await System.IO.File.WriteAllTextAsync(codePath, request.Code);

                    // Compile Java
                    var (compileExitCode, compileStdout, compileStderr) = await RunProcessAsync("javac", "Program.java", workingDir: sandboxDir, timeoutMs: 15000);
                    if (compileExitCode != 0)
                    {
                        compileSuccess = false;
                        compileError = compileStdout + "\n" + compileStderr;
                    }
                    else
                    {
                        executableName = "java";
                        runArgs = "Program";
                    }
                }
                else if (lang == "javascript" || lang == "js")
                {
                    var codePath = Path.Combine(sandboxDir, "index.js");
                    await System.IO.File.WriteAllTextAsync(codePath, request.Code);

                    executableName = "node";
                    runArgs = "index.js";
                }

                else
                {
                    return Json(new EjecutarCodigoResponse
                    {
                        Status = "Error",
                        Stderr = $"Lenguaje '{request.Language}' no soportado por el sandbox.",
                        ExecutionTimeMs = 0
                    });
                }

                if (!compileSuccess)
                {
                    return Json(new EjecutarCodigoResponse
                    {
                        Status = "Error",
                        Stderr = compileError,
                        ExecutionTimeMs = 0
                    });
                }

                // Determine test cases
                var testCases = new List<(string Input, string ExpectedOutput)>();
                if (request.TaskId.HasValue)
                {
                    if (request.TaskId.Value == Guid.Parse("66666666-6666-6666-6666-666666666666"))
                    {
                        testCases.Add(("5 7\n", "12"));
                        testCases.Add(("-3 10\n", "7"));
                    }
                    else if (request.TaskId.Value == Guid.Parse("77777777-7777-7777-7777-777777777777"))
                    {
                        testCases.Add(("hola\n", "aloh"));
                        testCases.Add(("StudyGo\n", "oGydutS"));
                    }
                }

                if (testCases.Count > 0)
                {
                    var stdoutResult = new StringBuilder();
                    stdoutResult.AppendLine("Ejecutando programa...");

                    int passed = 0;
                    long totalTime = 0;

                    for (int i = 0; i < testCases.Count; i++)
                    {
                        var tc = testCases[i];
                        var stopwatch = Stopwatch.StartNew();
                        var (runExitCode, runStdout, runStderr) = await RunProcessAsync(executableName, runArgs, tc.Input, workingDir: sandboxDir, timeoutMs: 10000);
                        stopwatch.Stop();
                        totalTime += stopwatch.ElapsedMilliseconds;

                        var cleanStdout = runStdout.Trim();
                        var cleanExpected = tc.ExpectedOutput.Trim();

                        if (runExitCode == 0 && cleanStdout == cleanExpected)
                        {
                            passed++;
                            stdoutResult.AppendLine($"Test Case {i + 1}/{testCases.Count}: OK");
                        }
                        else if (runExitCode != 0)
                        {
                            stdoutResult.AppendLine($"Test Case {i + 1}/{testCases.Count}: Error");
                            if (!string.IsNullOrWhiteSpace(runStderr))
                            {
                                stdoutResult.AppendLine($"   Runtime Error: {runStderr.Trim()}");
                            }
                        }
                        else
                        {
                            stdoutResult.AppendLine($"Test Case {i + 1}/{testCases.Count}: Fallo");
                            stdoutResult.AppendLine($"   Entrada: {tc.Input.Trim()}");
                            stdoutResult.AppendLine($"   Salida esperada: {cleanExpected}");
                            stdoutResult.AppendLine($"   Salida obtenida: {cleanStdout}");
                        }
                    }

                    return Json(new EjecutarCodigoResponse
                    {
                        Status = "Completado",
                        Stdout = stdoutResult.ToString(),
                        Stderr = "",
                        ExecutionTimeMs = (int)(totalTime / testCases.Count)
                    });
                }
                else
                {
                    // General playground execution
                    var stopwatch = Stopwatch.StartNew();
                    var (runExitCode, runStdout, runStderr) = await RunProcessAsync(executableName, runArgs, workingDir: sandboxDir, timeoutMs: 10000);
                    stopwatch.Stop();

                    if (runExitCode == 0)
                    {
                        return Json(new EjecutarCodigoResponse
                        {
                            Status = "Completado",
                            Stdout = runStdout,
                            Stderr = runStderr,
                            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                        });
                    }
                    else
                    {
                        return Json(new EjecutarCodigoResponse
                        {
                            Status = "Error",
                            Stdout = runStdout,
                            Stderr = runStderr,
                            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new EjecutarCodigoResponse
                {
                    Status = "Error",
                    Stderr = $"Error inesperado del sandbox: {ex.Message}",
                    ExecutionTimeMs = 0
                });
            }
            finally
            {
                // Cleanup directory
                try
                {
                    if (Directory.Exists(sandboxDir))
                    {
                        Directory.Delete(sandboxDir, true);
                    }
                }
                catch { }
            }
        }

        // POST: /Tareas/Entregas/{id}
        [HttpPost]
        public async Task<IActionResult> Entregar(Guid id, [FromBody] EntregarTareaRequest request)
        {
            var userId = GetCurrentUserId();
            var submission = await _academicService.GetOrCreateSubmissionAsync(id, userId);

            // Guardar versión
            var newVersion = await _academicService.SaveSubmissionVersionAsync(submission.Id, request.Code);
            // Marcar como entregado
            await _academicService.SubmitTaskAsync(submission.Id);

            return Json(new { success = true, versionId = newVersion.Id, versionNumber = newVersion.VersionNumber });
        }

        // GET: /Tareas/Revision/{id}?studentId={studentId}
        public async Task<IActionResult> Revision(Guid id, Guid? studentId = null)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();

            var task = await _academicService.GetTaskDetailAsync(id);
            if (task == null) return NotFound();

            var submissions = await _academicService.GetTaskSubmissionsAsync(id);

            var vm = new TareaRevisionViewModel
            {
                TaskId = task.Id,
                TaskTitle = task.Title,
                RubricTitle = task.Rubric != null ? "Rúbrica de la Tarea" : "Rúbrica General",
                SelectedStudentId = studentId,
                Submissions = submissions.Select(s => new StudentSubmissionItemViewModel
                {
                    StudentId = s.StudentId,
                    StudentName = s.Student?.DisplayName ?? "Estudiante",
                    StudentEmail = s.Student?.Email ?? "",
                    Status = s.Status.ToString(),
                    Score = s.Grade?.FinalScore,
                    LastUpdate = s.Versions.Any() ? s.Versions.Max(v => v.SavedAt) : (DateTime?)null
                }).ToList()
            };

            if (studentId.HasValue)
            {
                var selSub = submissions.FirstOrDefault(s => s.StudentId == studentId.Value);
                if (selSub != null)
                {
                    vm.SelectedSubmissionId = selSub.Id;
                    vm.SelectedSubmissionStatus = selSub.Status.ToString();
                    vm.FinalScore = selSub.Grade?.FinalScore;
                    vm.Feedback = "Excelente solución. Código limpio y eficiente."; // Feedback mock
                    
                    var versions = await _academicService.GetSubmissionVersionsAsync(selSub.Id);
                    vm.Versions = versions.Select(v => new VersionItemViewModel
                    {
                        Id = v.Id,
                        VersionNumber = v.VersionNumber,
                        SavedAt = v.SavedAt,
                        Status = selSub.Status == SubmissionStatus.Calificado || (selSub.Status == SubmissionStatus.Enviado && v.VersionNumber == versions.Max(x => x.VersionNumber)) ? "Oficial" : "En progreso"
                    }).ToList();

                    if (versions.Any())
                    {
                        vm.SelectedCode = versions.First().Code; // Código actual
                    }
                }
            }

            return View(vm);
        }

        // POST: /Tareas/Calificar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calificar(Guid submissionId, decimal score, string feedback, Guid taskId, Guid studentId)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();

            await _academicService.GradeSubmissionAsync(submissionId, score, feedback);
            return RedirectToAction(nameof(Revision), new { id = taskId, studentId = studentId });
        }
    }
}
