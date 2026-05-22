using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.LessonRequest;
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

        public LessonController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        // =========================================================================
        // 1. [ADMIN] TẠO MỚI BÀI HỌC LÝ THUYẾT
        // POST: api/Lesson
        // =========================================================================
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(LessonResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
        {
            if (request == null)
            {
                return BadRequest("Dữ liệu yêu cầu không được để trống.");
            }

            try
            {
                var result = await _lessonService.CreateLessonAsync(request);
                return CreatedAtAction(nameof(GetLessonById), new { lessonId = result.LessonId }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống khi tạo bài học.", detail = ex.Message });
            }
        }

        // =========================================================================
        // 2. [ADMIN/USER] LẤY DANH SÁCH TẤT CẢ BÀI HỌC (Sắp xếp theo Phase -> Module -> Lesson)
        // GET: api/Lesson
        // =========================================================================
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LessonResponse>))]
        public async Task<IActionResult> GetAllLessons()
        {
            try
            {
                var lessons = await _lessonService.GetAllLessonsAsync();
                return Ok(lessons);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống khi lấy danh sách bài học.", detail = ex.Message });
            }
        }

        // =========================================================================
        // 3. [ADMIN/USER] LẤY CHI TIẾT MỘT BÀI HỌC THEO ID
        // GET: api/Lesson/{lessonId}
        // =========================================================================
        [HttpGet("{lessonId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LessonResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLessonById(int lessonId)
        {
            try
            {
                var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
                if (lesson == null)
                {
                    return NotFound(new { message = $"Không tìm thấy bài học lý thuyết với ID {lessonId}" });
                }
                return Ok(lesson);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống khi lấy chi tiết bài học.", detail = ex.Message });
            }
        }

        // =========================================================================
        // 4. [USER] CẬP NHẬT TIẾN ĐỘ HỌC (Đánh dấu hoàn thành / hủy hoàn thành)
        // POST: api/Lesson/track-progress
        // =========================================================================
        [HttpPost("track-progress")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserLessonProgressResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TrackProgress([FromBody] UpdateLessonProgressRequest request)
        {
            if (request == null)
            {
                return BadRequest("Dữ liệu tiến độ không hợp lệ.");
            }

            try
            {
                var result = await _lessonService.TrackProgressAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống khi cập nhật tiến độ học tập.", detail = ex.Message });
            }
        }

        // =========================================================================
        // 5. [USER] LẤY TOÀN BỘ TIẾN ĐỘ HỌC LÝ THUYẾT CỦA MỘT USER
        // GET: api/Lesson/user-progress/{userId}
        // =========================================================================
        [HttpGet("user-progress/{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserLessonProgressResponse>))]
        public async Task<IActionResult> GetUserProgress(int userId)
        {
            try
            {
                var progress = await _lessonService.GetUserProgressAsync(userId);
                return Ok(progress);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống khi lấy tiến độ học viên.", detail = ex.Message });
            }
        }

        // =========================================================================
        // 6. [HÀM CỐT LÕI] KIỂM TRA ĐIỀU KIỆN USER CÓ ĐƯỢC VÀO CHIẾN DỊCH THỰC HÀNH KHÔNG
        // GET: api/Lesson/check-eligibility?userId=1&campaignId=5
        // =========================================================================
        [HttpGet("check-eligibility")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CheckCampaignEligibility([FromQuery] int userId, [FromQuery] int campaignId)
        {
            try
            {
                bool isEligible = await _lessonService.IsUserEligibleForCampaignAsync(userId, campaignId);
                return Ok(isEligible);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống khi kiểm tra điều kiện chiến dịch.", detail = ex.Message });
            }
        }

        // =========================================================================
        // 7. [ADMIN] CẤU HÌNH BÀI HỌC BẮT BUỘC CHO CHIẾN DỊCH
        // POST: api/Lesson/set-prerequisites
        // =========================================================================
        //[Authorize(Roles = "Admin")]
        [HttpPost("set-prerequisites")]
        public async Task<IActionResult> SetCampaignPrerequisites([FromBody] SetPrerequisitesRequest request)
        {
            if (request == null || request.LessonIds == null)
            {
                return BadRequest("Dữ liệu cấu hình không hợp lệ.");
            }

            try
            {
                await _lessonService.SetCampaignPrerequisitesAsync(request.CampaignId, request.LessonIds);
                return Ok(new { message = $"Đã cấu hình {request.LessonIds.Count} bài học bắt buộc cho chiến dịch {request.CampaignId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi khi cấu hình điều kiện.", detail = ex.Message });
            }
        }
    }
}