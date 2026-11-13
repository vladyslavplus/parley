namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Pools of English and Ukrainian demo names, cities and job titles. The pick is a deterministic bucket of the contact
/// number, so a re-seed gives the same person; the mix keeps both languages visible in the list.
/// </summary>
internal static class DemoNamePool
{
    public static IReadOnlyList<string> FirstNames { get; } =
    [
        "Олена", "Андрій", "Ірина", "Дмитро", "Наталія", "Олег", "Марія", "Сергій", "Юлія", "Павло",
        "Тетяна", "Максим", "Софія", "Богдан", "Катерина", "Артем", "Анна", "Ігор", "Вікторія", "Роман",
        "Emma", "James", "Olivia", "Noah", "Ava", "Liam", "Sophia", "William", "Mia", "Ethan",
        "Charlotte", "Lucas", "Amelia", "Henry", "Isabella", "Daniel", "Grace", "Jack", "Chloe", "Oliver",
    ];

    public static IReadOnlyList<string> LastNames { get; } =
    [
        "Коваленко", "Шевченко", "Мельник", "Ковальчук", "Петренко", "Ткаченко", "Бондаренко", "Лисенко",
        "Олійник", "Гончар", "Савченко", "Мороз", "Марченко", "Кравченко", "Бойко", "Кузьменко",
        "Smith", "Johnson", "Brown", "Taylor", "Wilson", "Davies", "Evans", "Thomas", "Roberts", "Walker",
        "Wright", "Robinson", "Thompson", "White", "Hughes", "Edwards", "Green", "Hall", "Wood", "Harris",
    ];

    public static IReadOnlyList<(string Country, string City)> Places { get; } =
    [
        ("UA", "Київ"), ("UA", "Львів"), ("UA", "Одеса"), ("UA", "Харків"), ("UA", "Дніпро"),
        ("GB", "London"), ("GB", "Manchester"), ("GB", "Leeds"), ("GB", "Bristol"),
        ("DE", "Berlin"), ("DE", "Hamburg"), ("DE", "Munich"),
        ("US", "New York"), ("US", "Chicago"), ("US", "Austin"),
        ("PL", "Warsaw"), ("PL", "Krakow"),
    ];

    public static IReadOnlyList<string> JobTitles { get; } =
    [
        "Head of Procurement", "Operations Manager", "Store Owner", "Purchasing Manager", "Category Buyer",
        "Finance Director", "Supply Chain Lead", "Retail Manager", "Account Manager", "Founder",
        "Marketing Manager", "Sales Director", "Office Manager", "Product Manager", "Logistics Coordinator",
    ];

    public static IReadOnlyList<string> CompanySuffixes { get; } =
    [
        "Trading", "Retail Group", "Wholesale", "Logistics", "Distribution", "Supply", "Market", "Partners",
    ];

    public static string First(int index) => Pick(FirstNames, index, "first");

    public static string Last(int index) => Pick(LastNames, index, "last");

    public static (string Country, string City) Place(int index) =>
        Places[DemoIdentifiers.Bucket(index.ToString(System.Globalization.CultureInfo.InvariantCulture), "place", Places.Count)];

    public static string JobTitle(int index) => Pick(JobTitles, index, "job");

    public static string CompanyName(int companyIndex) =>
        $"Demo {Pick(CompanySuffixes, companyIndex, "company")} {companyIndex + 1}";

    private static string Pick(IReadOnlyList<string> pool, int index, string decision) =>
        pool[DemoIdentifiers.Bucket(index.ToString(System.Globalization.CultureInfo.InvariantCulture), decision, pool.Count)];
}
