using ClosedXML.Excel;
using System.IO;

namespace IdScannerTool.Services;

public sealed class HistoryExcelExportService : IHistoryExcelExportService
{
    private const int NoticeRowCount = 4;
    private const int HeaderRowIndex = 5;
    private const int DataStartRowIndex = 6;

    private static readonly double[] ColumnWidths =
    {
        10, 60, 20, 20, 15, 15, 15, 15, 12, 12, 12, 20, 20, 20
    };

    private static readonly string[] NoticeLines =
    {
        "1. 이 문구는 삭제하지 마십시오. (데이터는 6행부터 처리됩니다.)",
        "2. * 표시가 있는 항목은 필수 입력 항목입니다.",
        "3. 작성방법 시트에 작성방법이 서술되어있습니다.",
        "4. 환전고객_실명구분_코드를 작성방법 시트 확인 후 2자리로 입력해 주시길 바랍니다."
    };

    private static readonly string[] GuideHeaders =
    {
        "연번", "항목명", "형식", "자리수", "항목내용 및 작성방법"
    };

    private static readonly string[] CountryCodeHeaders =
    {
        "국가명", "국가코드", "비고"
    };

    private static readonly (string No, string Name, string Format, string Length, string Description)[] GuideRows =
    {
        ("1", "환전일자", "숫자", "8", "ㅇ YYYYMMDD : \"-\" 등은 입력시 생략하고 숫자만 입력"),
        ("2", "환전고객명", "문자", "30이내", "ㅇ 환전고객명 ㅇ 확인할 수 없는 경우 NA 기재"),
        ("3", "환전고객_실명구분_코드", "숫자", "2", "ㅇ '01' : 주민등록번호, '02' : 여권번호, '03' : 외국인등록번호, '04' : 기타"),
        ("4", "환전고객_실명번호", "문자+숫자", "20이내", "ㅇ 여권번호, 주민등록번호, 외국인등록번호, 기타번호, 확인할 수 없는 경우 0000000000 기재"),
        ("5", "환전고객_국적부호", "문자", "2", "ㅇ 국가코드 2자리(국적코드 SHEET 참조), 확인할 수 없는 경우 ZZ 기재"),
        ("6", "환전구분_코드", "숫자", "1", "ㅇ '1' : 매입, '2' : 매각, '3' : 위탁매입, '4' : 위탁매각"),
        ("7", "통화구분_코드", "숫자", "1", "ㅇ '1' : 지폐, '2' : 주화, '3'  :T/C, '4' : 기타"),
        ("8", "통화종류_코드", "숫자", "2", "ㅇ '01' : 미화(USD), '02' : 엔화(JPY), '03' : 위안화(CNY), '04' : 유로화(EUR), '08' : 기타"),
        ("9", "거래금액", "숫자", "20이내", "ㅇ 소숫점 2자리까지 입력"),
        ("10", "매입율", "숫자", "10이내", "ㅇ 소숫점 2자리까지 입력"),
        ("11", "원화금액", "숫자", "20이내", "ㅇ 소숫점 2자리까지 입력"),
        ("12", "확인담당자", "문자", "10이내", "ㅇ 환전장부 작성 담당자"),
        ("13", "제출은행_지점_코드", "숫자", "7", "ㅇ 금융결제원 홈페이지(www.kftc.or.kr)>정보광장>금융회사코드조회 참조"),
        ("14", "환전교부_증명서번호", "숫자", "15", "ㅇ 환전 매각, 매입 증명서 일련번호 입력")
    };

    private const string CountryCodeData = """
가나	GH	Ghana
가봉	GA	Gabon
가이아나	GY	Guyana
감비아	GM	Gambia
건지	GG	Guernsey
과델로프	GP	Guadeloupe
과테말라	GT	Guatemala
괌	GU	Guam
교포	RA	
교황청	VA	Holy See (Vatican City State)
국제통화기금	Z1	International Monetary Fund
그레나다	GD	Grenada
그루지아	GE	Georgia
그리스	GR	Greece
그린랜드	GL	Greenland
기네비소	GW	Guinea-Bissau
기니	GN	Guinea
기타국	ZZ	
나미비아	NA	Namibia
나우르	NR	Nauru
나이지리아	NG	Nigeria
남수단	SS	South Sudan
남아프리카공화국	ZA	South Africa
남조지아 & 남샌드위치 군도	GS	South Georgia & the South Sandwich Islands
네덜란드	NL	Netherlands
네덜란드 열도	AN	Netherlands Antilles
네팔	NP	Nepal
노르웨이	NO	Norway
노폴크 아일랜드	NF	Norfolk Island
뉴 칼레도니아	NC	New Caledonia
뉴질랜드	NZ	New Zealand
니우에	NU	Niue
니제르	NE	Niger
니카라과	NI	Nicaragua
대만	TW	Taiwan, Province of China
덴마크	DK	Denmark
도미니카	DM	Dominica
도미니카 공화국	DO	Dominican Republic
독일	DE	Germany
드롬힝 마우드랜드	NQ	DROMHING MAUD LAND
라오스	LA	Lao People's Democratic Republic
라이베리아	LR	Liberia
라트비아	LV	Latvia
러시아 연방	RU	Russian Federation
레바논	LB	Lebanon
레소토	LS	Lesotho
루마니아	RO	Romania
루안다	RW	Rwanda
룩셈부르그	LU	Luxembourg
리비아	LY	Libyan Arab Jamahiriya
리투아니아	LT	Lithuania
리히텐슈타인	LI	Liechtenstein
마다카스카르	MG	Madagascar
마샬군도	MH	Marshall Islands
마세도니아	YM	macedonia
마세도니아	MK	Macedonia
마이너 아우틀링 합중국 군도	UM	United States Minor Outlying Islands
마이크로네시아	FM	Micronesia
마카오	MO	Macao
마티니크	MQ	Martinique
말라위	MW	Malawi
말레이지아	MY	Malaysia
말리	ML	Mali
맨섬	IM	Isle Of Man
메요트	YT	Mayotte
멕시코	MX	Mexico
모나코	MC	Monaco
모로코	MA	Morocco
모리셔스	MU	Mauritius
모리타니	MR	Mauritania
모잠비크	MZ	Mozambique
몬테네그로	ME	Republic of Montenegro
몬트세라트	MS	Montserrat
몰도바	MD	Moldova, Republic of
몰디브	MV	Maldives
몰타	MT	Malta
몽골	MN	Mongolia
미국	US	United States
미드웨이 군도	MI	Midway Islands
미령 버진군도	VI	Virgin Islands-U.S.
미얀마	MM	Myanmar
바누아투	VU	Vanuatu
바레인	BH	Bahrain
바베이도스	BB	Barbados
바하마	BS	Bahamas
방글라데시	BD	Bangladesh
버뮤다	BM	Bermuda
베네주엘라	VE	Venezuela
베닝	BJ	Benin
베라루스	BY	Belarus
베트남	VN	Viet nam
벨기에	BE	Belgium
벨리제	BZ	Belize
보빗군도	BV	Bouvet Island
보스니아-헤르체고비나	BA	Bosnia and Herzegovina
보츠와나	BW	Botswana
볼리비아	BO	Bolivia
부룬디	BI	Burundi
부르키나 파소	BF	Burkina Faso
부탄	BT	Bhutan
북마리아나 군도	MP	Northern Mariana Islands
북한	KP	Korea, Democratic People's Republic of
불가리아	BG	Bulgaria
불령 가이아나	GF	French Guiana
불령 남부지역	TF	French Southern Territories
불령 리유니온,코모도 제도	RE	Reunion
불령 폴리네시아	PF	French Polynesia
브라질	BR	Brazil
브루나이	BN	Brunei Darussalam
사모아	WS	Samoa
사우디아라비아	SA	Saudi Arabia
사이프러스	CY	Cyprus
산마리노	SM	San Marino
상토메 프린스페	ST	Sao Tome and Principe
서사하라	EH	Western Sahara
세네갈	SN	Senegal
세르비아	RS	Serbia
세르비아 와 몬테네그로	CS	Serbia and Montenegro
세이쉘	SC	Seychelles
세인트 루시아	LC	Saint Lucia
세인트 마틴	MF	Saint Martin
세인트 바르탤르미	BL	Saint Barthelemy
세인트 빈센트 그레나딘	VC	St. Vincent and the Grenadines
세인트 키츠 네비스	KN	Saint Kitts and Nevis
세인트 피레 미켈론	PM	St. Pierre et Miquelon
세인트 헬레나	SH	Saint Helena
소말리아	SO	Somalia
솔로몬 군도	SB	Solomon Islands
수단	SD	Sudan
수리남	SR	Suriname
스리랑카	LK	Srilanka
스발비드 군도	SJ	Svalbard and Jan Mayen
스와질랜드	SZ	Swaziland
스웨덴	SE	Sweden
스위스	CH	Switzerland
스페인	ES	Spain
슬로바키아	SK	Slovakia
슬로베니아	SI	Slovenia
시리아	SY	Syrian Arab Republic
시에라 리온	SL	Sierra Leone
싱가포르	SG	Singapore
아랍에미리트 연합	AE	United Arab Emirates
아루바	AW	Aruba
아르메니아	AM	Armenia
아르헨티나	AR	Argentina
아메리칸 사모아	AS	American Samoa
아이슬란드	IS	Iceland
아이티	HT	Haiti
아일랜드	IE	Ireland
아제르바이잔	AZ	Azerbaijan
아프카니스탄	AF	Afghanistan
안도라	AD	Andorra
안타티카	AQ	Antarctica
안티가 바부다	AG	Antigua and Barbuda
알랜드 군도	AX	Aland Islands
알바니아	AL	Albania
알제리	DZ	Algeria
앙골라	AO	Angola
앙귈라	AI	Anguilla
에리트리아	ER	Eritrea
에스토니아	EE	Estonia
에쿠아도르	EC	Ecuador
엘살바도르	SV	El Salvador
영국	GB	United Kingdom
영령 버진군도	VG	VirginnIslands -British
영령 안탁	BQ	British Antorc Territory
영령 인도양	IO	British Indian Ocean Territory
영령 캐이맨 군도	KY	Cayman Islands
예맨	YE	Yemen
오만	OM	Oman
오스트리아	AT	Austria
온두라스	HN	Honduras
왈라스 & 퓨투나 군도	WF	Wallis and futuna Islands
요르단	JO	Jordan
우간다	UG	Uganda
우루과이	UY	Uruguay
우즈베크	UZ	Uzbekistan
우크라이나	UA	Ukraine
웨이크 아일랜드	WK	Wake Island
유고	YU	Yugoslavia
이디오피아	ET	Ethiopia
이라크	IQ	Iraq
이란	IR	Iran -Islamic Republic of
이스라엘	IL	Israel
이집트	EG	Egypt
이탈리아	IT	Italy
인도	IN	India
인도네시아	ID	Indonesia
일본	JP	Japan
자마이카	JM	Jamaica
자이르	ZR	
잠비아	ZM	Zambia
저어지	JE	Jersey
적도 기니	GQ	Equatorial Guinea
존스톤 아일랜드	JT	Johnston Island
중국	CN	China
중립지대	NT	Neutral Zone
중앙아프리카공화국	CF	Central African Republic
지부티	DJ	Djibouti
지브랄타	GI	Gibraltar
짐바브웨	ZW	Zimbabwe
챠드	TD	Chad
체코공화국	CZ	Czech Republic
칠레	CL	Chile
카메룬	CM	Cameroon
카보 베르데	CV	Cape Verde
카자흐	KZ	Kazakhstan
카타르	QA	Qatar
캄보디아	KH	Cambodia
캐나다	CA	Canada
캔톤아일랜드	CT	Canton And Enderbury ISL
케냐	KE	Kenya
코모로스	KM	Comoros
코스 군도	CC	CocosKeeling Islands
코스타리카	CR	Costa Rica
코트디봐르	CI	Cote d'Ivoire
콜롬비아	CO	Colombia
콩고	CG	Congo
콩고민주공화국	CD	Congo, The Democratic Republic Of The
쿠바	CU	Cuba
쿠웨이트	KW	Kuwait
쿡 아일랜드	CK	Cook Islands
큐라소	CW	
크로아티아	HR	Croatia
크리스마스 아일랜드	CX	Christmas Island
키르기스	KG	Kyrgyzstan
키리바티	KI	Kiribati
타지크	TJ	Tajikistan
탄자니아	TZ	Tanzania,United Republic of
태국	TH	Thailand
터키	TR	Turkey
토고	TG	Togo
토켈라우	TK	Tokelau
통가	TO	Tonga
투르크 & 카이코스 군도	TC	Turks and Caicos Islands
투르크멘	TM	Turkmenistan
투발루	TV	Tuvalu
튀니지	TN	Tunisia
트리니다드 토바고	TT	Trinidad And Tobago
티모르	TL	Timor Leste
파나마	PA	Panama
파나마운하지역	PZ	Panama canal zone
파라과이	PY	Paraguay
파로에 군도	FO	Faroe Islands
파키스탄	PK	Pakistan
파푸아 뉴기니	PG	Papua New Guinea
팔라우	PW	Palau
팔레스타인 해방기구	PS	Palestinian Territory, Occupied
팔레스타인 해방기구	PO	PLO
페루	PE	Peru
포루투갈	PT	Portugal
포클랜드 군도	FK	Falkland Islands-Malvinas
폴란드	PL	Poland
푸에르토리코	PR	Puerto Rico
프랑스	FR	France
프랑스 메트로폴리탄	FX	France,Metropolitan
피지	FJ	Fiji
피트카이른	PN	Pitcairn
핀란드	FI	Finland
필리핀	PH	Philippines
한국	KR	Korea, Republic of
허드 앤 맥도날드 군도	HM	Heard Island and McDonald Islands
헝가리	HU	Hungary
호주	AU	Australia
홍콩	HK	Hong Kong
""";

    private static readonly string[] Headers =
    {
        "환전일자*",
        "환전고객명*",
        "환전고객_실명구분_코드*",
        "환전고객_실명번호*",
        "환전고객_국적부호*",
        "환전구분_코드*",
        "통화구분_코드*",
        "통화종류_코드*",
        "거래금액*",
        "매입률*",
        "원화금액*",
        "확인담당자",
        "제출은행_지점_코드*",
        "환전교부_증명서번호"
    };

    private static readonly HashSet<string> RedAsteriskExcludedHeaders = new(StringComparer.Ordinal)
    {
        "확인담당자",
        "환전교부_증명서번호"
    };

    public Task<string> ExportAsync(
        IReadOnlyList<HistoryExcelRow> rows,
        string filePath,
        CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("환전영업자_입력용");
            var guideWs = workbook.Worksheets.Add("작성방법");
            var countryCodeWs = workbook.Worksheets.Add("국적코드");

            ApplyNoticeArea(ws);
            ApplyGuideSheet(guideWs);
            ApplyCountryCodeSheet(countryCodeWs);

            for (var i = 0; i < Headers.Length; i++)
            {
                ws.Cell(HeaderRowIndex, i + 1).Value = Headers[i];
            }

            var rowIndex = DataStartRowIndex;
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ws.Cell(rowIndex, 1).Value = row.TimestampUtc.ToLocalTime().ToString("yyyyMMdd");
                ws.Cell(rowIndex, 2).Value = row.Name;
                ws.Cell(rowIndex, 3).Value = row.DocumentType;
                ws.Cell(rowIndex, 4).Value = row.DocumentNo;
                ws.Cell(rowIndex, 5).Value = row.Nationality;
                ws.Cell(rowIndex, 7).Value = string.Empty;
                ws.Cell(rowIndex, 8).Value = string.Empty;
                ws.Cell(rowIndex, 9).Value = string.Empty;
                ws.Cell(rowIndex, 10).Value = string.Empty;
                ws.Cell(rowIndex, 11).Value = string.Empty;
                ws.Cell(rowIndex, 12).Value = string.Empty;
                ws.Cell(rowIndex, 13).Value = string.Empty;
                ws.Cell(rowIndex, 14).Value = string.Empty;
                rowIndex++;
            }

            var tableRange = ws.Range(HeaderRowIndex, 1, Math.Max(rowIndex - 1, HeaderRowIndex), Headers.Length);
            tableRange.Style.Font.FontName = "맑은 고딕";
            tableRange.Style.Font.FontSize = 11;
            tableRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var headerRange = ws.Range(HeaderRowIndex, 1, HeaderRowIndex, Headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 10;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#c6d9f0");

            for (var i = 0; i < Headers.Length; i++)
            {
                ApplyHeaderRichText(ws.Cell(HeaderRowIndex, i + 1), Headers[i]);
            }

            for (var i = 0; i < ColumnWidths.Length && i < Headers.Length; i++)
            {
                ws.Column(i + 1).Width = ColumnWidths[i];
            }

            workbook.SaveAs(filePath);
            return filePath;
        }, cancellationToken);

    private static void ApplyNoticeArea(IXLWorksheet ws)
    {
        for (var i = 0; i < NoticeLines.Length; i++)
        {
            var rowIndex = i + 1;
            var range = ws.Range(rowIndex, 1, rowIndex, Headers.Length);
            range.Merge();
            range.Style.Font.FontName = "맑은 고딕";
            range.Style.Font.FontSize = 11;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#eeece1");
            range.Style.Alignment.WrapText = true;

            var cell = ws.Cell(rowIndex, 1);
            var richText = cell.GetRichText();
            richText.ClearText();

            if (rowIndex == 1)
            {
                richText.AddText("1. 이 문구는 삭제하지 마십시오. (데이터는 ");
                richText.AddText("6행").SetFontColor(XLColor.Red).SetBold();
                richText.AddText("부터 처리됩니다.)");
            }
            else if (rowIndex == 4)
            {
                richText.AddText("4. 환전고객_실명구분_코드를 작성방법 시트 확인 후 ");
                richText.AddText("2자리").SetFontColor(XLColor.Red).SetBold();
                richText.AddText("로 입력해 주시길 바랍니다.");
            }
            else
            {
                richText.AddText(NoticeLines[i]);
            }
        }
    }

    private static void ApplyHeaderRichText(IXLCell cell, string header)
    {
        if (string.IsNullOrEmpty(header) || RedAsteriskExcludedHeaders.Contains(header) || !header.Contains('*'))
        {
            return;
        }

        var textWithoutAsterisk = header.Replace("*", string.Empty, StringComparison.Ordinal);
        var richText = cell.GetRichText();
        richText.ClearText();
        richText.AddText(textWithoutAsterisk);
        richText.AddText("*").SetFontColor(XLColor.Red).SetBold();
    }

    private static void ApplyGuideSheet(IXLWorksheet ws)
    {
        for (var i = 0; i < GuideHeaders.Length; i++)
        {
            ws.Cell(1, i + 1).Value = GuideHeaders[i];
        }

        var rowIndex = 2;
        foreach (var row in GuideRows)
        {
            ws.Cell(rowIndex, 1).Value = row.No;
            ws.Cell(rowIndex, 2).Value = row.Name;
            ws.Cell(rowIndex, 3).Value = row.Format;
            ws.Cell(rowIndex, 4).Value = row.Length;
            ws.Cell(rowIndex, 5).Value = row.Description;
            rowIndex++;
        }

        var usedRange = ws.Range(1, 1, rowIndex - 1, GuideHeaders.Length);
        usedRange.Style.Font.FontName = "맑은 고딕";
        usedRange.Style.Font.FontSize = 11;
        usedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var headerRange = ws.Range(1, 1, 1, GuideHeaders.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontSize = 10;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#c6d9f0");

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 34;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 110;
        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        ws.Column(5).Style.Alignment.WrapText = true;
    }

    private static void ApplyCountryCodeSheet(IXLWorksheet ws)
    {
        for (var i = 0; i < CountryCodeHeaders.Length; i++)
        {
            ws.Cell(1, i + 1).Value = CountryCodeHeaders[i];
        }

        var rowIndex = 2;
        foreach (var row in ParseCountryCodeRows())
        {
            ws.Cell(rowIndex, 1).Value = row.CountryName;
            ws.Cell(rowIndex, 2).Value = row.CountryCode;
            ws.Cell(rowIndex, 3).Value = row.Note;
            rowIndex++;
        }

        var usedRange = ws.Range(1, 1, rowIndex - 1, CountryCodeHeaders.Length);
        usedRange.Style.Font.FontName = "맑은 고딕";
        usedRange.Style.Font.FontSize = 11;
        usedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var headerRange = ws.Range(1, 1, 1, CountryCodeHeaders.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontSize = 10;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#c6d9f0");

        ws.Column(1).Width = 28;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 48;
    }

    private static IEnumerable<(string CountryName, string CountryCode, string Note)> ParseCountryCodeRows()
    {
        foreach (var line in CountryCodeData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            var countryName = parts.Length > 0 ? parts[0] : string.Empty;
            var countryCode = parts.Length > 1 ? parts[1] : string.Empty;
            var note = parts.Length > 2 ? parts[2] : string.Empty;
            yield return (countryName, countryCode, note);
        }
    }
}
