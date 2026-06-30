using AntiPhisher.Application;
using AntiPhisher.Application.Request.PhaseRequest;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.PhaseResponse;
using AntiPhisher.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AntiPhisher.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhaseController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public PhaseController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/Phase  — danh sách tất cả phase kèm số module/lesson
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = new ApiResponse();
            try
            {
                var phases  = await _unitOfWork.Phases.GetAllAsync(null);
                var modules = await _unitOfWork.Modules.GetAllAsync(null);
                var lessons = await _unitOfWork.Lessons.GetAllAsync(null);

                var result = new List<PhaseDetailResponse>();
                foreach (var p in phases.OrderBy(p => p.OrderIndex))
                {
                    var phaseMods = modules.Where(m => m.PhaseId == p.PhaseId).ToList();
                    var modIds    = phaseMods.Select(m => m.ModuleId).ToHashSet();
                    int totalLessons = lessons.Count(l => modIds.Contains(l.ModuleId));

                    var moduleList = new List<ModuleSummary>();
                    foreach (var m in phaseMods.OrderBy(m => m.OrderIndex))
                    {
                        var modLessons = lessons
                            .Where(l => l.ModuleId == m.ModuleId)
                            .OrderBy(l => l.OrderIndex)
                            .Select(l => new LessonSummary { LessonId = l.LessonId, Title = l.Title, OrderIndex = l.OrderIndex, IsActive = l.IsActive })
                            .ToList();
                        moduleList.Add(new ModuleSummary
                        {
                            ModuleId    = m.ModuleId,
                            ModuleName  = m.ModuleName,
                            OrderIndex  = m.OrderIndex,
                            IsActive    = m.IsActive,
                            LessonCount = modLessons.Count,
                            Lessons     = modLessons
                        });
                    }

                    result.Add(new PhaseDetailResponse
                    {
                        PhaseId     = p.PhaseId,
                        PhaseName   = p.PhaseName,
                        Description = p.Description,
                        OrderIndex  = p.OrderIndex,
                        IsActive    = p.IsActive,
                        Color       = p.Color,
                        Icon        = p.Icon,
                        ModuleCount = phaseMods.Count,
                        LessonCount = totalLessons,
                        Modules     = moduleList
                    });
                }

                response.SetOk(result);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // POST: api/Phase  — tạo phase mới
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePhase([FromBody] CreatePhaseRequest request)
        {
            var response = new ApiResponse();
            try
            {
                int order = request.OrderIndex;
                if (order <= 0)
                {
                    var all = await _unitOfWork.Phases.GetAllAsync(null);
                    order = (all?.Count ?? 0) + 1;
                }

                var phase = new Phase
                {
                    PhaseName   = request.PhaseName.Trim(),
                    Description = request.Description?.Trim() ?? string.Empty,
                    OrderIndex  = order,
                    IsActive    = true,
                    Color       = request.Color,
                    Icon        = request.Icon
                };

                await _unitOfWork.Phases.AddAsync(phase);
                await _unitOfWork.SaveChangeAsync();

                response.SetOk(new PhaseDetailResponse
                {
                    PhaseId     = phase.PhaseId,
                    PhaseName   = phase.PhaseName,
                    Description = phase.Description,
                    OrderIndex  = phase.OrderIndex,
                    IsActive    = phase.IsActive,
                    Color       = phase.Color,
                    Icon        = phase.Icon
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // PUT: api/Phase/{id}  — cập nhật phase
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePhase(int id, [FromBody] UpdatePhaseRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var phase = await _unitOfWork.Phases.GetAsync(x => x.PhaseId == id);
                if (phase == null)
                {
                    response.SetNotFound(message: $"Không tìm thấy Phase ID {id}");
                    return NotFound(response);
                }

                if (request.PhaseName  != null) phase.PhaseName   = request.PhaseName.Trim();
                if (request.Description != null) phase.Description = request.Description.Trim();
                if (request.Color      != null) phase.Color       = request.Color;
                if (request.Icon       != null) phase.Icon        = request.Icon;
                if (request.OrderIndex != null) phase.OrderIndex  = request.OrderIndex.Value;
                if (request.IsActive   != null) phase.IsActive    = request.IsActive.Value;

                await _unitOfWork.SaveChangeAsync();
                response.SetOk(new { message = "Đã cập nhật phase" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // POST: api/Phase/{phaseId}/modules  — tạo module trong phase
        [Authorize(Roles = "Admin")]
        [HttpPost("{phaseId:int}/modules")]
        public async Task<IActionResult> CreateModule(int phaseId, [FromBody] CreateModuleRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var phase = await _unitOfWork.Phases.GetAsync(x => x.PhaseId == phaseId);
                if (phase == null)
                {
                    response.SetNotFound(message: $"Không tìm thấy Phase ID {phaseId}");
                    return NotFound(response);
                }

                int order = request.OrderIndex;
                if (order <= 0)
                {
                    var existing = await _unitOfWork.Modules.GetAllAsync(x => x.PhaseId == phaseId);
                    order = (existing?.Count ?? 0) + 1;
                }

                var module = new Module
                {
                    PhaseId    = phaseId,
                    ModuleName = request.ModuleName.Trim(),
                    OrderIndex = order,
                    IsActive   = true
                };

                await _unitOfWork.Modules.AddAsync(module);
                await _unitOfWork.SaveChangeAsync();

                response.SetOk(new ModuleSummary
                {
                    ModuleId   = module.ModuleId,
                    ModuleName = module.ModuleName,
                    OrderIndex = module.OrderIndex,
                    IsActive   = module.IsActive
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }
    }
}
