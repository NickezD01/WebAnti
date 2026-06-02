using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.LessonRequest;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.LessonResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AntiPhisher.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonController : ControllerBase
    {
        private readonly ILessonService _lessonService;
        private readonly IClaimService _claimService;

        public LessonController(ILessonService lessonService, IClaimService claimService)
        {
            _lessonService = lessonService;
            _claimService = claimService;
        }

        // =========================================================================
        // 1. [ADMIN] TẠO MỚI BÀI HỌC LÝ THUYẾT
        // POST: api/Lesson
        // =========================================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
        {
            var response = new ApiResponse();
            if (request == null)
            {
                response.SetBadRequest(message: "Dữ liệu yêu cầu không được để trống.");
                return BadRequest(response);
            }

            try
            {
                var result = await _lessonService.CreateLessonAsync(request);
                response.SetOk(result);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                response.SetNotFound(message: ex.Message);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // =========================================================================
        // 2. [ADMIN/USER] LẤY DANH SÁCH TẤT CẢ BÀI HỌC (Sắp xếp theo Phase -> Module -> Lesson)
        // GET: api/Lesson
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> GetAllLessons()
        {
            var response = new ApiResponse();
            try
            {
                var lessons = await _lessonService.GetAllLessonsAsync();
                response.SetOk(lessons);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // =========================================================================
        // 3. [ADMIN/USER] LẤY CHI TIẾT MỘT BÀI HỌC THEO ID
        // GET: api/Lesson/{lessonId}
        // =========================================================================
        [HttpGet("{lessonId:int}")]
        public async Task<IActionResult> GetLessonById(int lessonId)
        {
            var response = new ApiResponse();
            try
            {
                var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
                if (lesson == null)
                {
                    response.SetNotFound(message: $"Không tìm thấy bài học lý thuyết với ID {lessonId}");
                    return NotFound(response);
                }
                response.SetOk(lesson);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // =========================================================================
        // 4. [USER] CẬP NHẬT TIẾN ĐỘ HỌC (Đánh dấu hoàn thành / hủy hoàn thành)
        // POST: api/Lesson/track-progress
        // =========================================================================
        [Authorize]
        [HttpPost("track-progress")]
        public async Task<IActionResult> TrackProgress([FromBody] UpdateLessonProgressRequest request)
        {
            var response = new ApiResponse();
            if (request == null)
            {
                response.SetBadRequest(message: "Dữ liệu tiến độ không hợp lệ.");
                return BadRequest(response);
            }

            try
            {
                var claim = _claimService.GetUserClaim();
                var result = await _lessonService.TrackProgressAsync(claim.Id, request);
                response.SetOk(result);
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để cập nhật tiến độ." });
            }
            catch (KeyNotFoundException ex)
            {
                response.SetNotFound(message: ex.Message);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // =========================================================================
        // 5. [USER/MANAGER/ADMIN] LẤY TOÀN BỘ TIẾN ĐỘ HỌC LÝ THUYẾT CỦA MỘT USER
        // GET: api/Lesson/user-progress/{userId}
        // =========================================================================
        [Authorize]
        [HttpGet("user-progress/{userId:int}")]
        public async Task<IActionResult> GetUserProgress(int userId)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var progress = await _lessonService.GetUserProgressAsync(claim.Id, claim.Role, userId);
                response.SetOk(progress);
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                response.SetNotFound(message: ex.Message);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // =========================================================================
        // 6. [USER] KIỂM TRA ĐIỀU KIỆN USER CÓ ĐƯỢC VÀO CHIẾN DỊCH THỰC HÀNH KHÔNG
        // GET: api/Lesson/check-eligibility?campaignId=5
        // =========================================================================
        [Authorize]
        [HttpGet("check-eligibility")]
        public async Task<IActionResult> CheckCampaignEligibility([FromQuery] int campaignId)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                bool isEligible = await _lessonService.IsUserEligibleForCampaignAsync(claim.Id, campaignId);
                response.SetOk(isEligible);
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }
            catch (KeyNotFoundException ex)
            {
                response.SetNotFound(message: ex.Message);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // =========================================================================
        // 7. [ADMIN] CẤU HÌNH BÀI HỌC BẮT BUỘC CHO CHIẾN DỊCH
        // POST: api/Lesson/set-prerequisites
        // =========================================================================
        [Authorize(Roles = "Admin")]
        [HttpPost("set-prerequisites")]
        public async Task<IActionResult> SetCampaignPrerequisites([FromBody] SetPrerequisitesRequest request)
        {
            var response = new ApiResponse();
            if (request == null || request.LessonIds == null)
            {
                response.SetBadRequest(message: "Dữ liệu cấu hình không hợp lệ.");
                return BadRequest(response);
            }

            try
            {
                await _lessonService.SetCampaignPrerequisitesAsync(request.CampaignId, request.LessonIds);
                response.SetOk(new { message = $"Đã cấu hình {request.LessonIds.Count} bài học bắt buộc cho chiến dịch {request.CampaignId}" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // =========================================================================
        // 8. [USER] Lấy danh sách bài học được giao từ Campaign đang active
        // GET: api/Lesson/my-lessons
        // =========================================================================
        [Authorize]
        [HttpGet("my-lessons")]
        public async Task<IActionResult> GetMyLessons()
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var lessons = await _lessonService.GetMyLessonsAsync(claim.Id);
                response.SetOk(lessons);
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để xem bài học." });
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }
    }
}
