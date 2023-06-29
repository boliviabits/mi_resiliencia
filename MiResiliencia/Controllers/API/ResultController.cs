﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MiResiliencia.Models;
using MiResiliencia.Models.API;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MiResiliencia.Controllers.API
{
    [Route("api/[controller]/[action]/{id}")]
    [ApiController]
#if DEBUG
    //[Authorize]
#else
    [Authorize]
#endif
    public class ResultController : Controller
    {
        private readonly MiResilienciaContext _context;
        private readonly ILogger<ResultController> _logger;
        private readonly DamageExtentService _damageExtentService;
        //private readonly MappedObjectService _mappedObjectService;

        public ResultController(
            MiResilienciaContext context, 
            ILogger<ResultController> logger, 
            DamageExtentService damageExtentService
            //,
            //MappedObjectService mappedObjectService
            )
        {
            _context = context;
            _logger = logger;
            _damageExtentService = damageExtentService;
            //_mappedObjectService = mappedObjectService;
        }

        public async Task<ViewResult> SummaryAsync(int id, bool attachCss = false, bool details = false, bool print = false)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            ProjectResult _result = await _damageExtentService.ComposeProjectResultAsync(id, details);

            _logger.LogWarning($"ID {id.ToString().PadLeft(4)} - Project Result Summary: elapsed time = " + stopWatch.Elapsed.ToString());
            stopWatch.Stop();

            ViewBag.attachCss = attachCss;
            ViewBag.print = print;

            if (_result.ProcessResults.Any())
            {
                return View(_result);
            }
            else
            {
                return View("NoResult", _result);
            }
        }


        public async Task<ActionResult> DeleteAsync(int id)
        {
            await _damageExtentService.DeleteDamageExtentsFromDBAsync(id);

            return Content($"Deleted DamageExtents for project ID {id}");
        }

        public async Task<ActionResult> CreateAsync(int id)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            await _damageExtentService.CreateDamageExtent(id);

            _logger.LogWarning($"ID {id.ToString().PadLeft(4)} - Damage Extent Computed: elapsed time = " + stopWatch.Elapsed.ToString());
            stopWatch.Stop();

            return Content($"Computed DamageExtents for project ID {id}. Elapsed time = " + stopWatch.Elapsed.ToString());
        }

        /// <summary>
        /// Create and Summary
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attachCss"></param>
        /// <returns></returns>
        public async Task<ActionResult> RunAsync(int id, bool attachCss = false)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            await _damageExtentService.CreateDamageExtent(id);

            _logger.LogWarning($"ID {id.ToString().PadLeft(4)} - Damage Extent Computed: elapsed time = " + stopWatch.Elapsed.ToString());
            stopWatch.Restart();

            ProjectResult _result = await _damageExtentService.ComposeProjectResultAsync(id);

            _logger.LogWarning($"ID {id.ToString().PadLeft(4)} - Project Result Created: elapsed time = " + stopWatch.Elapsed.ToString());
            stopWatch.Stop();

            ViewBag.attachCss = attachCss;
            return View("Summary", _result);
        }

        public PartialViewResult ProcessScenarioChart(List<ScenarioResult> scenarioResults)
        {
            return PartialView(scenarioResults);
        }

        public PartialViewResult ProcessSummaryChart(ProcessResult processResult)
        {
            return PartialView(processResult);
        }

        public PartialViewResult ProjectChart(ProjectResult projectResult)
        {
            return PartialView(projectResult);
        }

        public PartialViewResult SummaryChart(ProjectResult projectResult)
        {
            return PartialView(projectResult);
        }


        [NonAction]
        public static async Task<bool> SetProjectStatusAsync(int projectId, int statusId)
        {
            if (projectId < 1 || statusId < 1 || statusId > 5)
                return false;

            string _setProjectStatusString =
                $"update \"Project\" " +
                $"set \"ProjectStateID\" = {statusId} " +
                $"where \"Id\"={projectId} ";

            var _context = new MiResilienciaContext();
            using (var transaction = _context.Database.BeginTransaction())
            {
                await _context.Database.ExecuteSqlRawAsync(_setProjectStatusString);
                await transaction.CommitAsync();
            }

            return true;
        }
    }
}