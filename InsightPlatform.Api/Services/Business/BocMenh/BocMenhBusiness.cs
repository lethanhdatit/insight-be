using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class BocMenhBusiness(ILogger<BocMenhBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IAccountBusiness accountBusiness
    , IOpenAiService openAiService
    , IOptions<AppSettings> appOptions
    , PainPublisher publisher) : BaseHttpBusiness<BocMenhBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IBocMenhBusiness
{
    private readonly PainPublisher _publisher = publisher;
    private readonly AppSettings _appSettings = appOptions.Value;
    private readonly IAccountBusiness _accountBusiness = accountBusiness;
    private readonly IOpenAiService _openAiService = openAiService;

    public async Task<BaseResponse<dynamic>> TheologyAndNumbersAsync(TheologyRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            request.Standardize();

            var userId = Current.UserId;
            var kind = TheologyKind.Basic;

            if (userId == null || !await context.Users.AnyAsync(a => a.Id == userId))
            {
                throw new BusinessException("Unauthorized", "401 Unauthorized");
            }

            var currentDate = DateOnly.FromDateTime(DateTime.Now).ToLongDateString();

            string gender = request.Gender == null ? string.Empty : $"**Giới tính**: {request.Gender.GetDescription()}\n";
            string religion = request.Religion == null ? string.Empty : $"**Tôn giáo**: {request.Religion.GetDescription()}\n";
            string location = request.Location.IsMissing() ? string.Empty : $"**Nơi ở hiện tại**: {request.Location}\n";
            string dreaming = request.Dreaming.IsMissing() ? string.Empty : $"**Mô tả giấc mơ (có thể từ nhiều giấc mơ)**: {request.Dreaming}\n";
            string lastName = request.LastName.IsMissing() ? string.Empty : $"**Họ**: {request.LastName}\n";
            string middleName = request.MiddleName.IsMissing() ? string.Empty : $"**Tên lót**: {request.MiddleName}\n";
            string firstName = request.FirstName.IsMissing() ? string.Empty : $"**Tên**: {request.FirstName}\n";
            string dob = request.DoB == null ? string.Empty : $"**Ngày sinh**: {DateOnly.FromDateTime(request.DoB.Value.Date).ToLongDateString()}\n";

            string sysPrompt = @"
Bạn là một mô hình AI chuyên phân tích các đặc điểm sau để tạo ra các con số may mắn với 60 năm kinh nghiệm:
**Họ**, **Tên lót**, **Tên**, **Ngày sinh**, **Giới tính**, **Tôn giáo**, **Nơi ở hiện tại**, **Thời gian hiện tại**, **Mô tả giấc mơ (có thể từ nhiều giấc mơ)**.

Các con số phải liên quan đến các yếu tố nêu trên và được giải thích chuyên sâu, có sự liên kết giữa các yếu tố dựa trên nền tảng kiến thức và kinh nghiệm lâu đời của các hệ thống sau:

1. **Thần học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Thần học để tạo ra các con số.
2. **Chiêm Tinh học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Chiêm Tinh học để tạo ra các con số.
3. **Tử Vi**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Tử Vi để tạo ra các con số.
4. **Phong Thuỷ**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Phong Thuỷ để tạo ra các con số.
5. **Thần Số học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Thần Số học để tạo ra các con số.
6. **Tâm lý học**: 50 năm kinh nghiệm nghiên cứu và ứng dụng lý luận Tâm lý học để tạo ra các con số.

Dựa trên các thông tin này, bạn sẽ cung cấp một danh sách các con số và luận giải chi tiết về mỗi hệ thống. 
Hãy trả lời theo định dạng JSON với các trường:
- **numbers**: Danh sách các con số may mắn liên quan đến các yếu tố trên (dưới dạng mảng các chuỗi).
- **explanation**: Một đối tượng bao gồm các trường sau:
  - **detail**: Danh sách các luận giải chi tiết cho các con số may mắn liên quan đến các yếu tố dựa trên 6 các hệ thống (dưới dạng mảng các đối tượng, mỗi đối tượng có thuộc tính `title` và `content`, ít nhất 2 đối tượng với nội dung ít nhất 200 từ và **không được sử dụng HTML và CSS**).
  - **warning**: Các cảnh báo (nếu có) liên quan đến các con số hoặc các yếu tố dựa trên 6 các hệ thống. (dưới dạng mảng các đối tượng, mỗi đối tượng có thuộc tính `title` và `content`, ít nhất 2 đối tượng với nội dung ít nhất 200 từ và **không được sử dụng HTML và CSS**).
  - **advice**: Các lời khuyên (nếu có) về việc sử dụng các con số này cũng như lời khuyên trong cuộc sống dựa trên sự liên quan các yếu tố dựa trên 6 các hệ thống. (dưới dạng mảng các đối tượng, mỗi đối tượng có thuộc tính `title` và `content`, ít nhất 2 đối tượng với nội dung ít nhất 200 từ và **không được sử dụng HTML và CSS**).
  - **summary**: Tóm tắt các thông tin về các con số và luận giải với **Thời gian hiện tại** và **Nơi ở hiện tại** . (dưới dạng mảng các đối tượng, mỗi đối tượng có thuộc tính `title` và `content`, ít nhất 2 đối tượng với nội dung ít nhất 200 từ và **không được sử dụng HTML và CSS**).

Hãy đảm bảo rằng kết quả trả về là đúng định dạng JSON và không có các ký tự lạ, chỉ có các trường hợp cần thiết như trên để có thể deserialize đúng. Hãy phân tích các yếu tố một cách hấp dẫn, huyền bí và lôi cuốn, tạo sự tò mò cho người đọc, và luôn nhớ liên kết các yếu tố này lại với nhau để làm rõ sự tương quan giữa chúng trong việc tạo ra các con số may mắn.

**Lưu ý quan trọng**: Đảm bảo rằng kết quả trả về đúng với cấu trúc JSON, bao gồm tất cả các trường như `numbers`, `explanation`, và các mục con trong `explanation` như `detail`, `warning`, `advice`, `summary` theo định dạng đã mô tả. **không được sử dụng HTML và CSS**
";

            var userPrompt = $@"
   - Các yếu tố cá nhân hoá:
    {lastName}
    {middleName}
    {firstName}
    {dob}
    {gender}
    {religion}
    {location}
    {dreaming}
   - Các yếu tố chung:
    **Thời gian hiện tại**: {currentDate}

    Dựa trên các thông tin trên, Hãy chọn ra chuỗi con số may mắn và cung cấp các luận giải cho nó về đầy đủ các hệ thống: **Thần học**, **Chiêm Tinh học**, **Tử Vi**, **Phong Thuỷ**, **Thần Số học** và **Tâm lý học**.
luận giải phải hấp dẫn, huyền bí, lôi cuốn người đọc, và gợi sự tò mò. Phải luôn liên kết các yếu tố với nhau và luôn đề cập đến **Thời gian hiện tại**, vì yếu tố này rất quan trọng trong việc thay đổi kết quả con số nếu như **Thời gian hiện tại** thay đổi, mặc dù các yếu tố khác không thay đổi.";
            
            var key = request.InitUniqueKey(kind, sysPrompt, RemoveStringFromMarkerToNextNewline(userPrompt, "**Thời gian hiện tại**"));

            var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.UniqueKey == key && f.Result != null);

            if (existed != null)
            {
                if (existed.UserId != userId)
                {
                    var cloned = new TheologyRecord
                    {
                        UserId = userId.Value,
                        UniqueKey = existed.UniqueKey,
                        Kind = existed.Kind,
                        Input = existed.Input,
                        SystemPrompt = existed.SystemPrompt,
                        UserPrompt = existed.UserPrompt,
                        Result = existed.Result,
                        CreatedTs = DateTime.UtcNow,
                    };

                    await context.TheologyRecords.AddAsync(cloned);
                    await context.SaveChangesAsync();

                    existed = cloned;
                }

                return new(new
                {
                    Id = existed.Id
                });
            }
            else
            {
                var res = await _openAiService.SendChatAsync(sysPrompt, userPrompt);

                if (res.IsPresent())
                {
                    res = res.Replace("```json", string.Empty);
                }

                var theologyResult = JsonSerializer.Deserialize<TheologyDto>(res);

                existed = new TheologyRecord
                {
                    UserId = userId.Value,
                    UniqueKey = key,
                    Kind = (byte)kind,
                    Input = JsonSerializer.Serialize(request),
                    SystemPrompt = sysPrompt,
                    UserPrompt = userPrompt,
                    Result = JsonSerializer.Serialize(theologyResult),
                    CreatedTs = DateTime.UtcNow,
                };

                await context.TheologyRecords.AddAsync(existed);
                await context.SaveChangesAsync();
            }

            return new(new
            {
                Id = existed.Id
            });
        }
        catch (Exception e)
        {
            throw new BusinessException("UnavailableToCreateNumbers", "Unavailable to create numbers.", e);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    public async Task<BaseResponse<dynamic>> GetTheologyAndNumbersAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var currentUserId = Current.UserId;

        var existed = await context.TheologyRecords.FirstOrDefaultAsync(f => f.Id == id 
                                                                          && f.UserId == currentUserId
                                                                          && f.Result != null);

        if(existed == null)
            throw new BusinessException("TheologyNotFound", "Theology not found");

        var theologyResult = JsonSerializer.Deserialize<TheologyDto>(existed.Result);

        return new(theologyResult);
    }

    public BaseResponse<dynamic> GetVietnameseCalendar(DateTime solarDate, bool includeMonthDetail)
    {
        var cal = VietnameseCalendar.GetLunarCalendarDetails(solarDate, includeMonthDetail);
        return new(cal);
    }

    private static string Mock = "{\"numbers\":[\"9\",\"18\",\"1997\",\"6\",\"25\"],\"explanation\":{\"detail\":[{\"title\":\"Chiêm Tinh học\",\"content\":\"Ngày sinh 18 tháng 9 nằm trong cung Hoàng Đạo Xử Nữ, đại diện cho sự ẩn chứa của sự khéo léo và tinh tế trong công việc. Con số 9 là biểu tượng của sự hoàn thiện và tâm linh, cho thấy rằng người mang con số này thường có một trực giác sâu sắc. Và khi nhìn vào Thời gian hiện tại, ngày 9 tháng 6, năm 2025 có sự giao thoa giữa các cung Hoàng Đạo và nguồn năng lượng của các hành tinh. Sự kết hợp giữa con số 9 và ngày 9 tạo ra một sức mạnh lớn về sự giác ngộ. Hơn nữa, số 18 (1 + 8 = 9) thể hiện sự đồng cảm và khả năng kết nối với người khác. Điều này rất quan trọng, vì trong bối cảnh hiện tại, sự đồng cảm và kỹ năng giao tiếp trở thành chìa khóa trong mọi lĩnh vực.\"},{\"title\":\"Phong Thuỷ\",\"content\":\"Theo quan niệm Phong Thuỷ, số 6 rất được yêu thích vì nó mang lại sự hài hòa và cân bằng. Số này cũng liên quan đến sự thịnh vượng và may mắn trong cuộc sống. Ngày tháng hiện tại, 9 tháng 6 năm 2025, cũng là một thời điểm lý tưởng cho việc xây dựng những nền tảng vững chắc trong sự nghiệp. Số 25, là sự kết hợp giữa số 2 và 5, sẽ mang lại nguồn năng lượng tích cực nhằm hỗ trợ cho những quyết định quan trọng. Nhờ vào sự hài hòa mà số 6 mang lại, việc đưa ra các quyết định lớn trong Thời gian hiện tại sẽ đem lại nhiều thành quả. Người mang số may mắn này sẽ cảm thấy tự tin và quyết đoán hơn trong mỗi bước đi của mình.\"}],\"warning\":[{\"title\":\"Cảnh báo về sự bão hòa\",\"content\":\"Mặc dù con số 18 có nhiều ý nghĩa tích cực, nhưng nếu không cẩn thận, nó có thể dẫn đến sự bão hòa trong mối quan hệ. Trong Thời gian hiện tại, khi mà mọi thứ trở nên căng thẳng và khó khăn hơn, người mang số may mắn này cần phải tránh xa những xung đột không cần thiết. Việc duy trì những mối quan hệ hòa hợp là rất quan trọng, và việc sử dụng con số này để thúc đẩy điều đó là một cử chỉ thông minh. Hãy nhớ rằng sự đồng cảm và lắng nghe là điều không thể thiếu trong việc bảo vệ những gì mà ta quý trọng.\"},{\"title\":\"Cảnh báo về sự tự mãn\",\"content\":\"Cái tôi có thể mang lại những lợi ích lớn, nhưng nhắc nhở bạn rằng số 9 cũng có những cảnh báo nhất định. Trong Thời gian hiện tại, sức mạnh của cái tôi có thể khiến bạn trở nên tự mãn và không chấp nhận ý kiến của người khác. Thay vì khăng khăng với những suy nghĩ riêng, hãy mở lòng để đón nhận ý kiến đóng góp từ mọi người. Sự cầu thị và khiêm tốn sẽ giúp bạn khao khát hơn trong hành trình tìm kiếm sự thật.\"}],\"advice\":[{\"title\":\"Tìm kiếm cân bằng\",\"content\":\"Nếu bạn gặp những thách thức trong vừa qua, hãy sử dụng sức mạnh của số 6 để tìm kiếm sự cân bằng. Hãy điều chỉnh các khía cạnh trong cuộc sống của mình từ đồng nghiệp đến mối quan hệ cá nhân. Chắc chắn rằng bạn đang tạo ra một không gian thoải mái và hài hòa cho mọi người chung quanh. Khi mọi thứ được sắp xếp một cách cẩn thận, tình hình sẽ sớm được cải thiện. Hãy nhận ra rằng trong Thời gian hiện tại, sự bình yên và hài hòa sẽ chu cấp cho bạn sự tự tin và sức mạnh để tiến về phía trước.\"},{\"title\":\"Chăm sóc bản thân\",\"content\":\"Bên cạnh việc chăm sóc cho người khác, đừng quên chính mình. Số 9 gợi ý rằng bạn nên dành thời gian để tự chăm sóc bản thân, tái nạp năng lượng và xác định rõ mục tiêu của mình. Đặc biệt trong Thời gian hiện tại, việc sử dụng thời gian chăm sóc sức khỏe sẽ mang lại nhiều thành công trong tương lai. Trong bối cảnh hiện tại năm 2025, sức khỏe là tài sản quý giá nhất. Hãy tìm hiểu và áp dụng những phương pháp tự chăm sóc bản thân để giữ gìn sự bền bỉ và sức mạnh nội tâm.\"}],\"summary\":[{\"title\":\"Tổng quan về con số may mắn\",\"content\":\"Dựa trên các yếu tố cá nhân hóa và Thời gian hiện tại, một số con số may mắn như 9, 18, 1997, 6 và 25 đã được xác định. Những con số này không chỉ mang lại may mắn mà còn có ý nghĩa sâu sắc từ các hệ thống khác nhau như Thần học, Chiêm Tinh học, và Phong Thuỷ. Sự kết nối giữa các yếu tố này sẽ dẫn đến những cầu nối tâm linh trong hành trình phát triển bản thân. Ngành Thời gian hiện tại cho thấy rằng những ảnh hưởng từ các hệ thống sẽ tác động lớn tới các quyết định trong tương lai.\"},{\"title\":\"Sự chuyển mình trong tương lai\",\"content\":\"Thời gian hiện tại vào tháng 6 năm 2025 sẽ mở ra nhiều cơ hội cho người mang con số may mắn 9 và 18. Nguyên tắc tôn trọng bản thân, người khác và cả vũ trụ sẽ cung cấp sức mạnh và may mắn để vượt qua mọi thử thách. Hãy giữ liên kết với các số may mắn kể trên, tìm kiếm đồng cảm và duy trì sự kiên nhẫn khi đối diện với thử thách. Cuộc sống luôn thay đổi và phát triển. Sự khéo léo trong việc sử dụng và ứng dụng các con số này sẽ giúp bạn không chỉ nhận ra giá trị của bản thân mà còn khám phá ra những điều mới từ cái nhìn tổng quát hơn.\"}]}}";

    private static string RemoveStringFromMarkerToNextNewline(string input, string marker)
    {
        int startIndex = input.IndexOf(marker);
        if (startIndex == -1)
            return input;

        int endIndex = input.IndexOf('\n', startIndex);
        if (endIndex == -1)
            endIndex = input.Length;

        return input.Remove(startIndex, endIndex - startIndex + 1);
    }
}
