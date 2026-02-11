use utf8;

package PortalCalendar::Integration::SvatkyAPI;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(decode_json encode_json);
use Mojo::URL;

use Try::Tiny;
use DateTime;
use Time::Seconds;

has 'lwp_max_cache_age' => 4 * ONE_HOUR;

has 'cs_bank_holidays' => sub {
    return {
        '0101' => 'Nový rok / Den obnovy samostatného českého státu',
        '0105' => 'Svátek práce',
        '0805' => 'Den vítězství',
        '0507' => 'Den slovanských věrozvěstů Cyrila a Metoděje',
        '0607' => 'Den upálení mistra Jana Husa',
        '2809' => 'Den české státnosti',
        '2810' => 'Den vzniku samostatného československého státu',
        '1711' => 'Den boje za svobodu a demokracii',
        '2412' => 'Štědrý den',
        '2512' => '1. svátek vánoční',
        '2612' => '2. svátek vánoční',
    };
};

has 'cs_name_days' => sub {
    return {
        '0101' => 'Nový rok',
        '0201' => 'Karina / Vasil',
        '0301' => 'Radmila / Radomil',
        '0401' => 'Diana',
        '0501' => 'Dalimil',
        '0601' => 'Tři králové',
        '0701' => 'Vilma',
        '0801' => 'Čestmír',
        '0901' => 'Vladan / Valtr',
        '1001' => 'Břetislav',
        '1101' => 'Bohdana',
        '1201' => 'Pravoslav',
        '1301' => 'Edita',
        '1401' => 'Radovan',
        '1501' => 'Alice',
        '1601' => 'Ctirad',
        '1701' => 'Drahoslav',
        '1801' => 'Vladislav',
        '1901' => 'Doubravka',
        '2001' => 'Ilona / Sebastián',
        '2101' => 'Běla',
        '2201' => 'Slavomír',
        '2301' => 'Zdeněk',
        '2401' => 'Milena',
        '2501' => 'Miloš',
        '2601' => 'Zora',
        '2701' => 'Ingrid',
        '2801' => 'Otýlie',
        '2901' => 'Zdislava',
        '3001' => 'Robin',
        '3101' => 'Marika',

        '0102' => 'Hynek',
        '0202' => 'Nela',
        '0302' => 'Blažej',
        '0402' => 'Jarmila',
        '0502' => 'Dobromila',
        '0602' => 'Vanda',
        '0702' => 'Veronika',
        '0802' => 'Milada',
        '0902' => 'Apolena',
        '1002' => 'Mojmír',
        '1102' => 'Božena',
        '1202' => 'Slavěna / Slávka',
        '1302' => 'Věnceslav / Věnceslava',
        '1402' => 'Valentýn / Valentýna',
        '1502' => 'Jiřina',
        '1602' => 'Ljuba',
        '1702' => 'Miloslava',
        '1802' => 'Gizela',
        '1902' => 'Patrik',
        '2002' => 'Oldřich',
        '2102' => 'Lenka',
        '2202' => 'Petr',
        '2302' => 'Svatopluk',
        '2402' => 'Matěj',
        '2502' => 'Liliana',
        '2602' => 'Dorota',
        '2702' => 'Alexandr',
        '2802' => 'Lumír',
        '2902' => 'Horymír',

        '0103' => 'Bedřich',
        '0203' => 'Anežka',
        '0303' => 'Kamil',
        '0403' => 'Stela',
        '0503' => 'Kazimír',
        '0603' => 'Miroslav',
        '0703' => 'Tomáš',
        '0803' => 'Gabriela',
        '0903' => 'Františka',
        '1003' => 'Viktorie',
        '1103' => 'Anděla',
        '1203' => 'Řehoř',
        '1303' => 'Růžena',
        '1403' => 'Rút / Matylda',
        '1503' => 'Ida',
        '1603' => 'Elena / Herbert',
        '1703' => 'Vlastimil',
        '1803' => 'Eduard',
        '1903' => 'Josef',
        '2003' => 'Světlana',
        '2103' => 'Radek',
        '2203' => 'Leona',
        '2303' => 'Ivona',
        '2403' => 'Gabriel',
        '2503' => 'Marián',
        '2603' => 'Emanuel',
        '2703' => 'Dita',
        '2803' => 'Soňa',
        '2903' => 'Taťána',
        '3003' => 'Arnošt',
        '3103' => 'Kvido',

        '0104' => 'Hugo',
        '0204' => 'Erika',
        '0304' => 'Richard',
        '0404' => 'Ivana',
        '0504' => 'Miroslava',
        '0604' => 'Vendula',
        '0704' => 'Heřman',
        '0804' => 'Ema',
        '0904' => 'Dušan',
        '1004' => 'Darja',
        '1104' => 'Izabela',
        '1204' => 'Julius',
        '1304' => 'Aleš',
        '1404' => 'Vincenc',
        '1504' => 'Anastázie',
        '1604' => 'Irena',
        '1704' => 'Rudolf',
        '1804' => 'Valérie',
        '1904' => 'Rostislav',
        '2004' => 'Marcela',
        '2104' => 'Alexandra',
        '2204' => 'Evženie',
        '2304' => 'Vojtěch',
        '2404' => 'Jiří',
        '2504' => 'Marek',
        '2604' => 'Oto',
        '2704' => 'Jaroslav',
        '2804' => 'Vlastislav',
        '2904' => 'Robert',
        '3004' => 'Blahoslav',

        '0105' => 'Svátek práce',
        '0205' => 'Zikmund',
        '0305' => 'Alex',
        '0405' => 'Květoslav',
        '0505' => 'Klaudie',
        '0605' => 'Radoslav',
        '0705' => 'Stanislav',
        '0805' => 'Den osvobození od fašismu (1945)',
        '0905' => 'Ctibor',
        '1005' => 'Blažena',
        '1105' => 'Svatava',
        '1205' => 'Pankrác',
        '1305' => 'Servác',
        '1405' => 'Bonifác',
        '1505' => 'Žofie / Sofie',
        '1605' => 'Přemysl',
        '1705' => 'Aneta',
        '1805' => 'Nataša',
        '1905' => 'Ivo',
        '2005' => 'Zbyšek',
        '2105' => 'Monika',
        '2205' => 'Emil',
        '2305' => 'Vladimír',
        '2405' => 'Jana',
        '2505' => 'Viola',
        '2605' => 'Filip',
        '2705' => 'Valdemar',
        '2805' => 'Vilém',
        '2905' => 'Maxmilián',
        '3005' => 'Ferdinand',
        '3105' => 'Kamila',

        '0106' => 'Laura',
        '0206' => 'Jarmil',
        '0306' => 'Tamara / Kevin',
        '0406' => 'Dalibor',
        '0506' => 'Dobroslav',
        '0606' => 'Norbert',
        '0706' => 'Iveta / Slavoj',
        '0806' => 'Medard',
        '0906' => 'Stanislava',
        '1006' => 'Gita / Margita',
        '1106' => 'Bruno',
        '1206' => 'Antonie',
        '1306' => 'Antonín',
        '1406' => 'Roland',
        '1506' => 'Vít',
        '1606' => 'Zbyněk',
        '1706' => 'Adolf',
        '1806' => 'Milan',
        '1906' => 'Leoš',
        '2006' => 'Květa',
        '2106' => 'Alois',
        '2206' => 'Pavla',
        '2306' => 'Zdeňka',
        '2406' => 'Jan',
        '2506' => 'Ivan',
        '2606' => 'Adriana',
        '2706' => 'Ladislav',
        '2806' => 'Lubomír',
        '2906' => 'Petr a Pavel',
        '3006' => 'Šárka',

        '0107' => 'Jaroslava',
        '0207' => 'Patricie',
        '0307' => 'Radomír',
        '0407' => 'Prokop',
        '0507' => 'Den slovanských věrozvěstů Cyrila a Metoděje',
        '0607' => 'Den upálení mistra Jana Husa (1415)',
        '0707' => 'Bohuslava',
        '0807' => 'Nora',
        '0907' => 'Drahoslava',
        '1007' => 'Libuše / Amálie',
        '1107' => 'Olga',
        '1207' => 'Bořek',
        '1307' => 'Markéta',
        '1407' => 'Karolína',
        '1507' => 'Jindřich',
        '1607' => 'Luboš',
        '1707' => 'Martina',
        '1807' => 'Drahomíra',
        '1907' => 'Čeněk',
        '2007' => 'Eliáš',
        '2107' => 'Vítězslav',
        '2207' => 'Magdaléna',
        '2307' => 'Libor',
        '2407' => 'Kristýna',
        '2507' => 'Jakub',
        '2607' => 'Anna',
        '2707' => 'Věroslav',
        '2807' => 'Viktor',
        '2907' => 'Marta',
        '3007' => 'Bořivoj',
        '3107' => 'Ignác',

        '0108' => 'Oskar',
        '0208' => 'Gustav',
        '0308' => 'Miluše',
        '0408' => 'Dominik',
        '0508' => 'Kristián',
        '0608' => 'Oldřiška',
        '0708' => 'Lada',
        '0808' => 'Soběslav',
        '0908' => 'Roman',
        '1008' => 'Vavřinec',
        '1108' => 'Zuzana',
        '1208' => 'Klára',
        '1308' => 'Alena',
        '1408' => 'Alan',
        '1508' => 'Hana',
        '1608' => 'Jáchym',
        '1708' => 'Petra',
        '1808' => 'Helena',
        '1908' => 'Ludvík',
        '2008' => 'Bernard',
        '2108' => 'Johana',
        '2208' => 'Bohuslav',
        '2308' => 'Sandra',
        '2408' => 'Bartoloměj',
        '2508' => 'Radim',
        '2608' => 'Luděk',
        '2708' => 'Otakar',
        '2808' => 'Augustýn',
        '2908' => 'Evelína',
        '3008' => 'Vladěna',
        '3108' => 'Pavlína',

        '0109' => 'Linda / Samuel',
        '0209' => 'Adéla',
        '0309' => 'Bronislav',
        '0409' => 'Jindřiška / Rozálie',
        '0509' => 'Boris',
        '0609' => 'Boleslav',
        '0709' => 'Regína',
        '0809' => 'Mariana',
        '0909' => 'Daniela',
        '1009' => 'Irma',
        '1109' => 'Denis',
        '1209' => 'Marie',
        '1309' => 'Lubor',
        '1409' => 'Radka',
        '1509' => 'Jolana',
        '1609' => 'Ludmila',
        '1709' => 'Naděžda',
        '1809' => 'Kryštof',
        '1909' => 'Zita',
        '2009' => 'Oleg',
        '2109' => 'Matouš',
        '2209' => 'Darina',
        '2309' => 'Berta',
        '2409' => 'Jaromír',
        '2509' => 'Zlata',
        '2609' => 'Andrea',
        '2709' => 'Jonáš',
        '2809' => 'Václav',
        '2909' => 'Michal',
        '3009' => 'Jeroným',

        '0110' => 'Igor',
        '0210' => 'Olívie',
        '0310' => 'Bohumil',
        '0410' => 'František',
        '0510' => 'Eliška',
        '0610' => 'Hanuš',
        '0710' => 'Justýna',
        '0810' => 'Věra',
        '0910' => 'Štefan',
        '1010' => 'Marina',
        '1110' => 'Andrej',
        '1210' => 'Marcel',
        '1310' => 'Renáta',
        '1410' => 'Agáta',
        '1510' => 'Tereza',
        '1610' => 'Havel',
        '1710' => 'Hedvika',
        '1810' => 'Lukáš',
        '1910' => 'Michaela',
        '2010' => 'Vendelín',
        '2110' => 'Brigita',
        '2210' => 'Sabina',
        '2310' => 'Teodor',
        '2410' => 'Nina',
        '2510' => 'Beáta',
        '2610' => 'Erik',
        '2710' => 'Šarlota / Zoe',
        '2810' => 'Alfréd',
        '2910' => 'Silvie',
        '3010' => 'Tadeáš',
        '3110' => 'Štěpánka',

        '0111' => 'Felix',
        '0211' => 'Tobiáš',
        '0311' => 'Hubert',
        '0411' => 'Karel',
        '0511' => 'Miriam',
        '0611' => 'Liběna',
        '0711' => 'Saskie',
        '0811' => 'Bohumír',
        '0911' => 'Bohdan',
        '1011' => 'Evžen',
        '1111' => 'Martin',
        '1211' => 'Benedikt',
        '1311' => 'Tibor',
        '1411' => 'Sáva',
        '1511' => 'Leopold',
        '1611' => 'Otmar',
        '1711' => 'Mahulena / Gertruda',
        '1811' => 'Romana',
        '1911' => 'Alžběta',
        '2011' => 'Nikola',
        '2111' => 'Albert',
        '2211' => 'Cecílie',
        '2311' => 'Klement',
        '2411' => 'Emílie',
        '2511' => 'Kateřina',
        '2611' => 'Artur',
        '2711' => 'Xenie',
        '2811' => 'René',
        '2911' => 'Zina',
        '3011' => 'Ondřej',

        '0112' => 'Iva',
        '0212' => 'Blanka',
        '0312' => 'Svatoslav',
        '0412' => 'Barbora',
        '0512' => 'Jitka',
        '0612' => 'Mikuláš',
        '0712' => 'Ambrož',
        '0812' => 'Květoslava',
        '0912' => 'Vratislav',
        '1012' => 'Julie',
        '1112' => 'Dana',
        '1212' => 'Simona',
        '1312' => 'Lucie',
        '1412' => 'Lýdie',
        '1512' => 'Radana',
        '1612' => 'Albína',
        '1712' => 'Daniel',
        '1812' => 'Miloslav',
        '1912' => 'Ester',
        '2012' => 'Dagmar',
        '2112' => 'Natálie',
        '2212' => 'Šimon',
        '2312' => 'Vlasta',
        '2412' => 'Adam a Eva, Štědrý den',
        '2512' => 'Boží hod vánoční, 1.svátek vánoční',
        '2612' => 'Štěpán, 2.svátek vánoční',
        '2712' => 'Žaneta',
        '2812' => 'Bohumila',
        '2912' => 'Judita',
        '3012' => 'David',
        '3112' => 'Silvestr',
    };
};

sub raw_details_from_web {
    my $self = shift;
    my $date = shift;

    return $self->local_variant($date);
}

sub local_variant {
    my $self = shift;
    my $date = shift;

    my $ret = {
        date        => $date->ymd('-'),
        isHoliday   => 0,
        dayNumber   => $date->day,
        monthNumber => $date->month,
        year        => $date->year,
        dayInWeek   => $date->day_name(),
        name        => $self->cs_name_days->{
            sprintf( "%02d%02d", $date->day, $date->month )
        },
        month => {
            genitive   => $date->month_name(),
            nominative => '?',                   # to be filled later
        }
    };

    my $month_cs_genitive_to_nominative = {
        'ledna'     => 'leden',
        'února'     => 'únor',
        'března'    => 'březen',
        'dubna'     => 'duben',
        'května'    => 'květen',
        'června'    => 'červen',
        'července'  => 'červenec',
        'srpna'     => 'srpen',
        'září'      => 'září',
        'října'     => 'říjen',
        'listopadu' => 'listopad',
        'prosince'  => 'prosinec',
    };
    $ret->{month}->{nominative} =
      $month_cs_genitive_to_nominative->{ $ret->{month}->{genitive} }
      // $ret->{month}->{genitive};

    # DDMM format for easy matching
    my $daykey = sprintf( "%02d%02d", $ret->{dayNumber}, $ret->{monthNumber} );

    if ( exists $self->cs_bank_holidays->{$daykey} ) {
        $ret->{isHoliday}   = 1;
        $ret->{holidayName} = $self->cs_bank_holidays->{$daykey};
    }

   # Calculate easter-related holidays (Easter Sunday, Easter Monday, Pentecost)
    my $easter_sunday =
      DateTime->new( year => $ret->{year}, month => 3, day => 21 )
      ->add( days => ( ( 19 * ( $ret->{year} % 19 ) + 24 ) % 30 ) + 1 );
    if ( $date->ymd('-') eq $easter_sunday->add( days => 1 )->ymd('-') ) {
        $ret->{isHoliday}   = 1;
        $ret->{holidayName} = 'Velikonoční pondělí';
    }
    elsif ( $date->ymd('-') eq $easter_sunday->add( days => -2 )->ymd('-') ) {
        $ret->{isHoliday}   = 1;
        $ret->{holidayName} = 'Velký pátek';
    }

    return encode_json($ret);
}

sub transform_details {
    my $self     = shift;
    my $raw_text = shift;

    my $raw = decode_json($raw_text);

    my $ret = {
        date    => $raw->{date},
        as_bool => {
            holiday => $raw->{isHoliday},
        },
        as_number => {
            day   => $raw->{dayNumber},
            month => $raw->{monthNumber},
            year  => $raw->{year},
        },
        as_text => {
            day_of_week => $raw->{dayInWeek},
            month       => {
                nominative => $raw->{month}->{nominative},
                genitive   => $raw->{month}->{genitive},
            },
            name    => $raw->{name},
            holiday => $raw->{holidayName},
        }
    };

    return $ret;
}

sub get_today_details {
    my $self = shift;
    my $date = shift // DateTime->now();

    my $cache = $self->db_cache;
    $cache->max_age( 1 * ONE_DAY );

    my $ret;
    try {
        $ret = $cache->get_or_set(
            sub {
                my $raw       = $self->raw_details_from_web($date);
                my $processed = $self->transform_details($raw);
                return $processed;
            },
            { date => $date->ymd('-') }    # date only, ignore the time
        );
    }
    catch {
        $self->app->log->error(
            "Error fetching data for date " . $date->ymd('-') . ": $_" );
        $ret = {};
    };

    return $ret;
}

1;
