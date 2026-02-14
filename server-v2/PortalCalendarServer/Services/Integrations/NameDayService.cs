using System.Globalization;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Service for retrieving Czech name days (Sv ·tky).
/// Provides a centralized repository of Czech name day information.
/// </summary>
public class NameDayService
{
    private readonly ILogger<NameDayService> _logger;

    private static readonly Dictionary<string, string> CzechNameDays = new()
    {
        // January
        { "0101", "Nov˝ rok" },
        { "0201", "Karina" },
        { "0301", "Radmila" },
        { "0401", "Diana" },
        { "0501", "Dalimil" },
        { "0601", "T¯i kr·lovÈ" },
        { "0701", "Vilma" },
        { "0801", "»estmÌr" },
        { "0901", "Vladan+Valtr" },
        { "1001", "B¯etislav" },
        { "1101", "Bohdana" },
        { "1201", "Pravoslav" },
        { "1301", "Edita" },
        { "1401", "Radovan" },
        { "1501", "Alice" },
        { "1601", "Ctirad" },
        { "1701", "Drahoslav" },
        { "1801", "Vladislav" },
        { "1901", "Doubravka" },
        { "2001", "Ilona+Sebasti·n" },
        { "2101", "BÏla" },
        { "2201", "SlavomÌr" },
        { "2301", "ZdenÏk" },
        { "2401", "Milena" },
        { "2501", "Miloö" },
        { "2601", "Zora" },
        { "2701", "Ingrid" },
        { "2801", "Ot˝lie" },
        { "2901", "Zdislava" },
        { "3001", "Robin" },
        { "3101", "Marika" },

        // February
        { "0102", "Hynek" },
        { "0202", "Nela" },
        { "0302", "Blaûej" },
        { "0402", "Jarmila" },
        { "0502", "Dobromila" },
        { "0602", "Vanda" },
        { "0702", "Veronika" },
        { "0802", "Milada" },
        { "0902", "Apolena" },
        { "1002", "MojmÌr" },
        { "1102", "Boûena" },
        { "1202", "Sl·vka" },
        { "1302", "VÏnceslav" },
        { "1402", "Valent˝n" },
        { "1502", "Ji¯ina" },
        { "1602", "Ljuba" },
        { "1702", "Miloslava" },
        { "1802", "Gizela" },
        { "1902", "Patrik" },
        { "2002", "Old¯ich" },
        { "2102", "Lenka" },
        { "2202", "Petr" },
        { "2302", "Svatopluk" },
        { "2402", "MatÏj" },
        { "2502", "Liliana" },
        { "2602", "Dorota" },
        { "2702", "Alexandr" },
        { "2802", "LumÌr" },
        { "2902", "HorymÌr" },

        // March
        { "0103", "Bed¯ich" },
        { "0203", "Aneûka" },
        { "0303", "Kamil" },
        { "0403", "Stela" },
        { "0503", "KazimÌr" },
        { "0603", "Miroslav" },
        { "0703", "Tom·ö" },
        { "0803", "Gabriela" },
        { "0903", "Frantiöka" },
        { "1003", "Viktorie" },
        { "1103", "AndÏla" },
        { "1203", "ÿeho¯" },
        { "1303", "R˘ûena" },
        { "1403", "R˙t+Matylda" },
        { "1503", "Ida" },
        { "1603", "Elena+Herbert" },
        { "1703", "Vlastimil" },
        { "1803", "Eduard" },
        { "1903", "Josef" },
        { "2003", "SvÏtlana" },
        { "2103", "Radek" },
        { "2203", "Leona" },
        { "2303", "Ivona" },
        { "2403", "Gabriel" },
        { "2503", "Mari·n" },
        { "2603", "Emanuel" },
        { "2703", "Dita" },
        { "2803", "SoÚa" },
        { "2903", "Taù·na" },
        { "3003", "Arnoöt" },
        { "3103", "Kvido" },

        // April
        { "0104", "Hugo" },
        { "0204", "Erika" },
        { "0304", "Richard" },
        { "0404", "Ivana" },
        { "0504", "Miroslava" },
        { "0604", "Vendula" },
        { "0704", "He¯man" },
        { "0804", "Ema" },
        { "0904", "Duöan" },
        { "1004", "Darja" },
        { "1104", "Izabela" },
        { "1204", "Julius" },
        { "1304", "Aleö" },
        { "1404", "Vincenc" },
        { "1504", "Anast·zie" },
        { "1604", "Irena" },
        { "1704", "Rudolf" },
        { "1804", "ValÈrie" },
        { "1904", "Rostislav" },
        { "2004", "Marcela" },
        { "2104", "Alexandra" },
        { "2204", "Evûenie" },
        { "2304", "VojtÏch" },
        { "2404", "Ji¯Ì" },
        { "2504", "Marek" },
        { "2604", "Oto" },
        { "2704", "Jaroslav" },
        { "2804", "Vlastislav" },
        { "2904", "Robert" },
        { "3004", "Blahoslav" },

        // May
        { "0105", "Sv·tek pr·ce" },
        { "0205", "Zikmund" },
        { "0305", "Alex" },
        { "0405", "KvÏtoslav" },
        { "0505", "Klaudie" },
        { "0605", "Radoslav" },
        { "0705", "Stanislav" },
        { "0805", "Den osvobozenÌ od faöismu (1945)" },
        { "0905", "Ctibor" },
        { "1005", "Blaûena" },
        { "1105", "Svatava" },
        { "1205", "Pankr·c" },
        { "1305", "Serv·c" },
        { "1405", "Bonif·c" },
        { "1505", "éofie+Sofie" },
        { "1605", "P¯emysl" },
        { "1705", "Aneta" },
        { "1805", "Nataöa" },
        { "1905", "Ivo" },
        { "2005", "Zbyöek" },
        { "2105", "Monika" },
        { "2205", "Emil" },
        { "2305", "VladimÌr" },
        { "2405", "Jana" },
        { "2505", "Viola" },
        { "2605", "Filip" },
        { "2705", "Valdemar" },
        { "2805", "VilÈm" },
        { "2905", "Maxmili·n" },
        { "3005", "Ferdinand" },
        { "3105", "Kamila" },

        // June
        { "0106", "Laura" },
        { "0206", "Jarmil" },
        { "0306", "Tamara" },
        { "0406", "Dalibor" },
        { "0506", "Dobroslav" },
        { "0606", "Norbert" },
        { "0706", "Iveta" },
        { "0806", "Medard" },
        { "0906", "Stanislava" },
        { "1006", "Gita" },
        { "1106", "Bruno" },
        { "1206", "Antonie" },
        { "1306", "AntonÌn" },
        { "1406", "Roland" },
        { "1506", "VÌt" },
        { "1606", "ZbynÏk" },
        { "1706", "Adolf" },
        { "1806", "Milan" },
        { "1906", "Leoö" },
        { "2006", "KvÏta" },
        { "2106", "Alois" },
        { "2206", "Pavla" },
        { "2306", "ZdeÚka" },
        { "2406", "Jan" },
        { "2506", "Ivan" },
        { "2606", "Adriana" },
        { "2706", "Ladislav" },
        { "2806", "LubomÌr" },
        { "2906", "Petr a Pavel" },
        { "3006", "ä·rka" },

        // July
        { "0107", "Jaroslava" },
        { "0207", "Patricie" },
        { "0307", "RadomÌr" },
        { "0407", "Prokop" },
        { "0507", "Den slovansk˝ch vÏrozvÏst˘ Cyrila a MetodÏje" },
        { "0607", "Den up·lenÌ mistra Jana Husa (1415)" },
        { "0707", "Bohuslava" },
        { "0807", "Nora" },
        { "0907", "Drahoslava" },
        { "1007", "Libuöe+Am·lie" },
        { "1107", "Olga" },
        { "1207", "Bo¯ek" },
        { "1307", "MarkÈta" },
        { "1407", "KarolÌna" },
        { "1507", "Jind¯ich" },
        { "1607", "Luboö" },
        { "1707", "Martina" },
        { "1807", "DrahomÌra" },
        { "1907", "»enÏk" },
        { "2007", "Eli·ö" },
        { "2107", "VÌtÏzslav" },
        { "2207", "MagdalÈna" },
        { "2307", "Libor" },
        { "2407", "Krist˝na" },
        { "2507", "Jakub" },
        { "2607", "Anna" },
        { "2707", "VÏroslav" },
        { "2807", "Viktor" },
        { "2907", "Marta" },
        { "3007", "Bo¯ivoj" },
        { "3107", "Ign·c" },

        // August
        { "0108", "Oskar" },
        { "0208", "Gustav" },
        { "0308", "Miluöe" },
        { "0408", "Dominik" },
        { "0508", "Kristi·n" },
        { "0608", "Old¯iöka" },
        { "0708", "Lada" },
        { "0808", "SobÏslav" },
        { "0908", "Roman" },
        { "1008", "Vav¯inec" },
        { "1108", "Zuzana" },
        { "1208", "Kl·ra" },
        { "1308", "Alena" },
        { "1408", "Alan" },
        { "1508", "Hana" },
        { "1608", "J·chym" },
        { "1708", "Petra" },
        { "1808", "Helena" },
        { "1908", "LudvÌk" },
        { "2008", "Bernard" },
        { "2108", "Johana" },
        { "2208", "Bohuslav" },
        { "2308", "Sandra" },
        { "2408", "BartolomÏj" },
        { "2508", "Radim" },
        { "2608", "LudÏk" },
        { "2708", "Otakar" },
        { "2808", "August˝n" },
        { "2908", "EvelÌna" },
        { "3008", "VladÏna" },
        { "3108", "PavlÌna" },

        // September
        { "0109", "Linda" },
        { "0209", "AdÈla" },
        { "0309", "Bronislav" },
        { "0409", "Jind¯iöka" },
        { "0509", "Boris" },
        { "0609", "Boleslav" },
        { "0709", "RegÌna" },
        { "0809", "Mariana" },
        { "0909", "Daniela" },
        { "1009", "Irma" },
        { "1109", "Denis" },
        { "1209", "Marie" },
        { "1309", "Lubor" },
        { "1409", "Radka" },
        { "1509", "Jolana" },
        { "1609", "Ludmila" },
        { "1709", "NadÏûda" },
        { "1809", "Kryötof" },
        { "1909", "Zita" },
        { "2009", "Oleg" },
        { "2109", "Matouö" },
        { "2209", "Darina" },
        { "2309", "Berta" },
        { "2409", "JaromÌr" },
        { "2509", "Zlata" },
        { "2609", "Andrea" },
        { "2709", "Jon·ö" },
        { "2809", "V·clav" },
        { "2909", "Michal" },
        { "3009", "Jeron˝m" },

        // October
        { "0110", "Igor" },
        { "0210", "OlÌvie" },
        { "0310", "Bohumil" },
        { "0410", "Frantiöek" },
        { "0510", "Eliöka" },
        { "0610", "Hanuö" },
        { "0710", "Just˝na" },
        { "0810", "VÏra" },
        { "0910", "ätefan" },
        { "1010", "Marina" },
        { "1110", "Andrej" },
        { "1210", "Marcel" },
        { "1310", "Ren·ta" },
        { "1410", "Ag·ta" },
        { "1510", "Tereza" },
        { "1610", "Havel" },
        { "1710", "Hedvika" },
        { "1810", "Luk·ö" },
        { "1910", "Michaela" },
        { "2010", "VendelÌn" },
        { "2110", "Brigita" },
        { "2210", "Sabina" },
        { "2310", "Teodor" },
        { "2410", "Nina" },
        { "2510", "Be·ta" },
        { "2610", "Erik" },
        { "2710", "äarlota+Zoe" },
        { "2810", "AlfrÈd" },
        { "2910", "Silvie" },
        { "3010", "Tade·ö" },
        { "3110", "ätÏp·nka" },

        // November
        { "0111", "Felix" },
        { "0211", "Tobi·ö" },
        { "0311", "Hubert" },
        { "0411", "Karel" },
        { "0511", "Miriam" },
        { "0611", "LibÏna" },
        { "0711", "Saskie" },
        { "0811", "BohumÌr" },
        { "0911", "Bohdan" },
        { "1011", "Evûen" },
        { "1111", "Martin" },
        { "1211", "Benedikt" },
        { "1311", "Tibor" },
        { "1411", "S·va" },
        { "1511", "Leopold" },
        { "1611", "Otmar" },
        { "1711", "Mahulena+Gertruda" },
        { "1811", "Romana" },
        { "1911", "AlûbÏta" },
        { "2011", "Nikola" },
        { "2111", "Albert" },
        { "2211", "CecÌlie" },
        { "2311", "Klement" },
        { "2411", "EmÌlie" },
        { "2511", "Kate¯ina" },
        { "2611", "Artur" },
        { "2711", "Xenie" },
        { "2811", "RenÈ" },
        { "2911", "Zina" },
        { "3011", "Ond¯ej" },

        // December
        { "0112", "Iva" },
        { "0212", "Blanka" },
        { "0312", "Svatoslav" },
        { "0412", "Barbora" },
        { "0512", "Jitka" },
        { "0612", "Mikul·ö" },
        { "0712", "Ambroû" },
        { "0812", "KvÏtoslava" },
        { "0912", "Vratislav" },
        { "1012", "Julie" },
        { "1112", "Dana" },
        { "1212", "Simona" },
        { "1312", "Lucie" },
        { "1412", "L˝die" },
        { "1512", "Radana" },
        { "1612", "AlbÌna" },
        { "1712", "Daniel" },
        { "1812", "Miloslav" },
        { "1912", "Ester" },
        { "2012", "Dagmar" },
        { "2112", "Nat·lie" },
        { "2212", "äimon" },
        { "2312", "Vlasta" },
        { "2412", "Adam a Eva, ätÏdr˝ den" },
        { "2512", "BoûÌ hod v·noËnÌ, 1.sv·tek v·noËnÌ" },
        { "2612", "ätÏp·n, 2.sv·tek v·noËnÌ" },
        { "2712", "éaneta" },
        { "2812", "Bohumila" },
        { "2912", "Judita" },
        { "3012", "David" },
        { "3112", "Silvestr" }
    };

    public NameDayService(ILogger<NameDayService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get name day information for a specific date in Czech calendar.
    /// Returns null if no name day is celebrated on this date.
    /// </summary>
    /// <param name="date">The date to lookup</param>
    /// <param name="countryCode">Country code (currently only "CZ" is supported)</param>
    /// <returns>Name day information or null if not found</returns>
    public NameDayInfo? GetNameDay(DateTime date, string countryCode = "CZ")
    {
        if (countryCode != "CZ")
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}. Only 'CZ' is currently supported.", countryCode);
            return null;
        }

        _logger.LogDebug("Looking up name day for {Date} in {CountryCode} calendar", date, countryCode);

        var key = date.ToString("ddMM", CultureInfo.InvariantCulture);
        if (CzechNameDays.TryGetValue(key, out var name))
        {
            _logger.LogDebug("Found name day: {Name} for date {Date}", name, date);
            return new NameDayInfo
            {
                Name = name,
                Date = date,
                CountryCode = countryCode
            };
        }

        _logger.LogDebug("No name day found for date {Date}", date);
        return null;
    }

    /// <summary>
    /// Get all name days for a specific month
    /// </summary>
    /// <param name="year">Year</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="countryCode">Country code</param>
    /// <returns>List of name days in the specified month</returns>
    public List<NameDayInfo> GetNameDaysForMonth(int year, int month, string countryCode = "CZ")
    {
        if (countryCode != "CZ")
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}", countryCode);
            return new List<NameDayInfo>();
        }

        var nameDays = new List<NameDayInfo>();
        var daysInMonth = DateTime.DaysInMonth(year, month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var nameDay = GetNameDay(date, countryCode);
            if (nameDay != null)
            {
                nameDays.Add(nameDay);
            }
        }

        return nameDays;
    }
}

/// <summary>
/// Name day information for Czech calendar
/// </summary>
public class NameDayInfo
{
    /// <summary>
    /// Name(s) celebrated on this day
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Date of the name day
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Country code (e.g., "CZ" for Czech Republic)
    /// </summary>
    public string CountryCode { get; set; } = "CZ";

    /// <summary>
    /// Additional description or alternative names
    /// </summary>
    public string? Description { get; set; }
}
