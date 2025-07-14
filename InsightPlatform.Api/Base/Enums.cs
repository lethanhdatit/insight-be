using System.ComponentModel;

public enum Gender
{
    [Description("Nam")]
    Male = 1,

    [Description("Nữ")]
    Female = 2,

    [Description("Phi nhị nguyên")]
    NonBinary = 3,

    [Description("Nam chuyển giới")]
    TransgenderMale = 4,

    [Description("Nữ chuyển giới")]
    TransgenderFemale = 5,

    [Description("Biến đổi giới tính")]
    GenderFluid = 6,

    [Description("Vô giới")]
    Agender = 7,

    [Description("Hai linh hồn")]
    TwoSpirit = 8,

    [Description("Intersex")]
    Intersex = 9,

    [Description("Khác")]
    Other = 10
}

public enum Religion
{
    [Description("Phật giáo")]
    Buddhism = 1,

    [Description("Công giáo")]
    Catholic = 2,

    [Description("Tin Lành")]
    Protestant = 3,
   
    [Description("Hồi giáo")]
    Islam = 4,

    [Description("Do Thái giáo")]
    Judaism = 5,

    [Description("Ấn Độ giáo")]
    Hinduism = 6,

    [Description("Khổng giáo")]
    Confucianism = 7,

    [Description("Tao giáo")]
    Taoism = 8,

    [Description("Vô thần")]
    Atheism = 9,

    [Description("Tôn giáo khác")]
    Other = 10
}

public enum TheologyKind : byte
{
    [Description("Cơ bản")]
    Basic = 1,

    [Description("Luận giải bát tự tứ trụ")]
    TuTruBatTu = 2,
}

public enum TheologyStatus : byte
{
    Created = 1,
    Analyzing = 2,
    Analyzed = 3,
}

public enum TopUpPackageKind : byte
{
    [Description("Vô Vi")]
    Package1 = 1,

    [Description("Vô Ngã")]
    Package2 = 2,

    [Description("Vô Tướng")]
    Package3 = 3,

    [Description("Vô Úy")]
    Package4 = 4,

    [Description("Vô Nhiễm")]
    Package5 = 5,

    [Description("Vô lượng")]
    Package6 = 6,
}

public enum TopupPackageStatus : byte
{
    Actived = 1,
    Inactived = 2,
    Deleted = 3,
}

//Description|Icon|IsActive
public enum TransactionProvider : byte
{
    [Description("VietQR VN")]
    VietQR = 1,

    [Description("Paypal")]
    Paypal = 2,
}

public enum TransactionStatus : byte
{
    New = 1,
    Processing = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5,
}