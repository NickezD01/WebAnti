using AntiPhisher.Application.DataSeeding;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class DataSeederService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DataSeederService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task SeedScenariosFromFolderAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Thư mục chứa file JSON seeding không tồn tại.");
                return;
            }

            // Lấy tất cả các file JSON kịch bản có trong thư mục chỉ định
            var jsonFiles = Directory.GetFiles(folderPath, "batch_*.json");

            foreach (var file in jsonFiles)
            {
                try
                {
                    string jsonContent = await File.ReadAllTextAsync(file);
                    var batchData = JsonSerializer.Deserialize<ScenarioJsonBatch>(jsonContent);

                    if (batchData == null || batchData.Simulations == null) continue;

                    Console.WriteLine($"Đang xử lý file: {Path.GetFileName(file)} - {batchData.BatchName}");

                    foreach (var sim in batchData.Simulations)
                    {
                        // 1. Kiểm tra xem Scenario này đã tồn tại trong DB chưa tránh trùng lặp khi chạy lại
                        // Giả định bạn có thể check theo Subject hoặc một trường tương đương
                        var isExist = await _unitOfWork.Scenarios.GetAsync(s => s.Title == sim.Email.Subject);
                        if (isExist != null) continue;

                        // 2. Xác định DifficultyId dựa theo LevelName trong DB
                        // (Ví dụ: easy -> Id 1, medium -> Id 2, hard -> Id 3)
                        int difficultyId = 1;
                        if (sim.Difficulty.ToLower() == "medium") difficultyId = 2;
                        else if (sim.Difficulty.ToLower() == "hard") difficultyId = 3;

                        // 3. Khởi tạo Entity Scenario khớp hoàn toàn với cấu trúc logic AutoMapper của dự án
                        var scenario = new Scenario
                        {
                            Title = sim.Email.Subject,
                            Description = $"Kịch bản mô phỏng Email Phishing gửi từ nguồn giả mạo: {sim.Email.SenderEmail}",
                            SenderName = sim.Email.SenderEmail.Contains("@") ? sim.Email.SenderEmail.Split('@')[0] : "Hệ thống",
                            RecipientName = "Học viên hệ thống",
                            ExplanationHint = "Hãy chú ý kiểm tra kỹ địa chỉ Email người gửi (Domain mạo danh), các liên kết ẩn khi hover chuột và tính cấp bách thúc giục trong nội dung.",

                            // Các trường mặc định/bổ sung trong database
                            CategoryId = 1,
                            DifficultyId = difficultyId,
                            IsActive = true,
                            IsAIGenerated = false,
                            AttachmentUrl = string.Empty,

                            // Nếu DB của bạn cần lưu thêm thông tin nội dung Email phục vụ cho việc render hiển thị
                            // Thêm các thuộc tính này nếu thực thể Scenario trong Domain của bạn có hỗ trợ
                            // MailBody = sim.Email.Body, 
                            // SenderEmail = sim.Email.SenderEmail
                        };

                        await _unitOfWork.Scenarios.AddAsync(scenario);
                    }

                    // Lưu thay đổi cho từng batch file để tránh quá tải bộ nhớ và dễ theo dõi log
                    await _unitOfWork.SaveChangeAsync();
                    Console.WriteLine($" Thành công: Đã seeding kịch bản từ {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Lỗi khi xử lý file {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }
    }
}
