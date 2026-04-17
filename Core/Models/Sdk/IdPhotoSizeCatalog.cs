using System;
using System.Collections.Generic;

namespace HivisionIDPhotos.Core.Models.Sdk;

public static class IdPhotoSizeCatalog
{
    public static readonly IdPhotoPixelSize OneInch = new(295, 413, "One inch");
    public static readonly IdPhotoPixelSize TwoInch = new(413, 626, "Two inches");
    public static readonly IdPhotoPixelSize SmallOneInch = new(260, 378, "Small one inch");
    public static readonly IdPhotoPixelSize SmallTwoInch = new(413, 531, "Small two inches");
    public static readonly IdPhotoPixelSize LargeOneInch = new(390, 567, "Large one inch");
    public static readonly IdPhotoPixelSize LargeTwoInch = new(413, 626, "Large two inches");
    public static readonly IdPhotoPixelSize FiveInchPhoto = new(1050, 1499, "Five inches");
    public static readonly IdPhotoPixelSize TeacherQualificationCertificate = new(295, 413, "Teacher qualification certificate");
    public static readonly IdPhotoPixelSize NationalCivilServiceExam = new(295, 413, "National civil service exam");
    public static readonly IdPhotoPixelSize PrimaryAccountingExam = new(295, 413, "Primary accounting exam");
    public static readonly IdPhotoPixelSize EnglishCet = new(144, 192, "English CET-4 and CET-6 exams");
    public static readonly IdPhotoPixelSize ComputerLevelExam = new(390, 567, "Computer level exam");
    public static readonly IdPhotoPixelSize GraduateEntranceExam = new(531, 709, "Graduate entrance exam");
    public static readonly IdPhotoPixelSize SocialSecurityCard = new(358, 441, "Social security card");
    public static readonly IdPhotoPixelSize ElectronicDriversLicense = new(260, 378, "Electronic driver's license");
    public static readonly IdPhotoPixelSize AmericanVisa = new(600, 600, "American visa");
    public static readonly IdPhotoPixelSize JapaneseVisa = new(295, 413, "Japanese visa");
    public static readonly IdPhotoPixelSize KoreanVisa = new(413, 531, "Korean visa");

    private static readonly IReadOnlyDictionary<string, IdPhotoPixelSize> SizeMap =
        new Dictionary<string, IdPhotoPixelSize>(StringComparer.OrdinalIgnoreCase)
        {
            ["one_inch"] = OneInch,
            ["two_inches"] = TwoInch,
            ["small_one_inch"] = SmallOneInch,
            ["small_two_inches"] = SmallTwoInch,
            ["large_one_inch"] = LargeOneInch,
            ["large_two_inches"] = LargeTwoInch,
            ["five_inches"] = FiveInchPhoto,
            ["teacher_qualification_certificate"] = TeacherQualificationCertificate,
            ["national_civil_service_exam"] = NationalCivilServiceExam,
            ["primary_accounting_exam"] = PrimaryAccountingExam,
            ["english_cet"] = EnglishCet,
            ["computer_level_exam"] = ComputerLevelExam,
            ["graduate_entrance_exam"] = GraduateEntranceExam,
            ["social_security_card"] = SocialSecurityCard,
            ["electronic_drivers_license"] = ElectronicDriversLicense,
            ["american_visa"] = AmericanVisa,
            ["japanese_visa"] = JapaneseVisa,
            ["korean_visa"] = KoreanVisa
        };

    public static bool TryGet(string key, out IdPhotoPixelSize size)
    {
        return SizeMap.TryGetValue(key ?? string.Empty, out size!);
    }
}
