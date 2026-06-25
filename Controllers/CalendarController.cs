using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace StudyGo.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetEvents(string start, string end)
        {
            // TODO: Consumir Application Service filtrando por fechas y curso/usuario
            var events = new List<object>
            {
                new {
                    id = "1",
                    title = "Entrega: Tarea Java",
                    start = "2026-06-25T23:59:00",
                    className = "event-warning",
                    extendedProps = new { type = "entrega", course = "Programación II" }
                },
                new {
                    id = "2",
                    title = "Quiz: Álgebra Booleana",
                    start = "2026-06-26T10:00:00",
                    end = "2026-06-26T12:00:00",
                    className = "event-purple",
                    extendedProps = new { type = "examen", course = "Matemáticas Discretas" }
                }
            };
            return Json(events);
        }
    }
}