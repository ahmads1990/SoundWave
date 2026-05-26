using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedCountries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AF", "Afghanistan" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AL", "Albania" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "Name" },
                values: new object[] { "DZ", "Algeria" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AD", "Andorra" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AO", "Angola" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AG", "Antigua and Barbuda" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AR", "Argentina" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AM", "Armenia" });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "Countries",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 9, "AU", "Australia" },
                    { 10, "AT", "Austria" },
                    { 11, "AZ", "Azerbaijan" },
                    { 12, "BS", "Bahamas" },
                    { 13, "BH", "Bahrain" },
                    { 14, "BD", "Bangladesh" },
                    { 15, "BB", "Barbados" },
                    { 16, "BY", "Belarus" },
                    { 17, "BE", "Belgium" },
                    { 18, "BZ", "Belize" },
                    { 19, "BJ", "Benin" },
                    { 20, "BT", "Bhutan" },
                    { 21, "BO", "Bolivia" },
                    { 22, "BA", "Bosnia and Herzegovina" },
                    { 23, "BW", "Botswana" },
                    { 24, "BR", "Brazil" },
                    { 25, "BN", "Brunei" },
                    { 26, "BG", "Bulgaria" },
                    { 27, "BF", "Burkina Faso" },
                    { 28, "BI", "Burundi" },
                    { 29, "CV", "Cabo Verde" },
                    { 30, "KH", "Cambodia" },
                    { 31, "CM", "Cameroon" },
                    { 32, "CA", "Canada" },
                    { 33, "CF", "Central African Republic" },
                    { 34, "TD", "Chad" },
                    { 35, "CL", "Chile" },
                    { 36, "CN", "China" },
                    { 37, "CO", "Colombia" },
                    { 38, "KM", "Comoros" },
                    { 39, "CG", "Congo" },
                    { 40, "CD", "Congo, Democratic Republic" },
                    { 41, "CR", "Costa Rica" },
                    { 42, "CI", "Cote d Ivoire" },
                    { 43, "HR", "Croatia" },
                    { 44, "CU", "Cuba" },
                    { 45, "CY", "Cyprus" },
                    { 46, "CZ", "Czech Republic" },
                    { 47, "DK", "Denmark" },
                    { 48, "DJ", "Djibouti" },
                    { 49, "DM", "Dominica" },
                    { 50, "DO", "Dominican Republic" },
                    { 51, "EC", "Ecuador" },
                    { 52, "EG", "Egypt" },
                    { 53, "SV", "El Salvador" },
                    { 54, "GQ", "Equatorial Guinea" },
                    { 55, "ER", "Eritrea" },
                    { 56, "EE", "Estonia" },
                    { 57, "SZ", "Eswatini" },
                    { 58, "ET", "Ethiopia" },
                    { 59, "FJ", "Fiji" },
                    { 60, "FI", "Finland" },
                    { 61, "FR", "France" },
                    { 62, "GA", "Gabon" },
                    { 63, "GM", "Gambia" },
                    { 64, "GE", "Georgia" },
                    { 65, "DE", "Germany" },
                    { 66, "GH", "Ghana" },
                    { 67, "GR", "Greece" },
                    { 68, "GD", "Grenada" },
                    { 69, "GT", "Guatemala" },
                    { 70, "GN", "Guinea" },
                    { 71, "GW", "Guinea-Bissau" },
                    { 72, "GY", "Guyana" },
                    { 73, "HT", "Haiti" },
                    { 74, "HN", "Honduras" },
                    { 75, "HU", "Hungary" },
                    { 76, "IS", "Iceland" },
                    { 77, "IN", "India" },
                    { 78, "ID", "Indonesia" },
                    { 79, "IR", "Iran" },
                    { 80, "IQ", "Iraq" },
                    { 81, "IE", "Ireland" },
                    { 82, "IL", "Israel" },
                    { 83, "IT", "Italy" },
                    { 84, "JM", "Jamaica" },
                    { 85, "JP", "Japan" },
                    { 86, "JO", "Jordan" },
                    { 87, "KZ", "Kazakhstan" },
                    { 88, "KE", "Kenya" },
                    { 89, "KI", "Kiribati" },
                    { 90, "KP", "Korea, North" },
                    { 91, "KR", "Korea, South" },
                    { 92, "KW", "Kuwait" },
                    { 93, "KG", "Kyrgyzstan" },
                    { 94, "LA", "Laos" },
                    { 95, "LV", "Latvia" },
                    { 96, "LB", "Lebanon" },
                    { 97, "LS", "Lesotho" },
                    { 98, "LR", "Liberia" },
                    { 99, "LY", "Libya" },
                    { 100, "LI", "Liechtenstein" },
                    { 101, "LT", "Lithuania" },
                    { 102, "LU", "Luxembourg" },
                    { 103, "MG", "Madagascar" },
                    { 104, "MW", "Malawi" },
                    { 105, "MY", "Malaysia" },
                    { 106, "MV", "Maldives" },
                    { 107, "ML", "Mali" },
                    { 108, "MT", "Malta" },
                    { 109, "MH", "Marshall Islands" },
                    { 110, "MR", "Mauritania" },
                    { 111, "MU", "Mauritius" },
                    { 112, "MX", "Mexico" },
                    { 113, "FM", "Micronesia" },
                    { 114, "MD", "Moldova" },
                    { 115, "MC", "Monaco" },
                    { 116, "MN", "Mongolia" },
                    { 117, "ME", "Montenegro" },
                    { 118, "MA", "Morocco" },
                    { 119, "MZ", "Mozambique" },
                    { 120, "MM", "Myanmar" },
                    { 121, "NA", "Namibia" },
                    { 122, "NR", "Nauru" },
                    { 123, "NP", "Nepal" },
                    { 124, "NL", "Netherlands" },
                    { 125, "NZ", "New Zealand" },
                    { 126, "NI", "Nicaragua" },
                    { 127, "NE", "Niger" },
                    { 128, "NG", "Nigeria" },
                    { 129, "MK", "North Macedonia" },
                    { 130, "NO", "Norway" },
                    { 131, "OM", "Oman" },
                    { 132, "PK", "Pakistan" },
                    { 133, "PW", "Palau" },
                    { 134, "PA", "Panama" },
                    { 135, "PG", "Papua New Guinea" },
                    { 136, "PY", "Paraguay" },
                    { 137, "PE", "Peru" },
                    { 138, "PH", "Philippines" },
                    { 139, "PL", "Poland" },
                    { 140, "PT", "Portugal" },
                    { 141, "QA", "Qatar" },
                    { 142, "RO", "Romania" },
                    { 143, "RU", "Russia" },
                    { 144, "RW", "Rwanda" },
                    { 145, "KN", "Saint Kitts and Nevis" },
                    { 146, "LC", "Saint Lucia" },
                    { 147, "VC", "Saint Vincent and the Grenadines" },
                    { 148, "WS", "Samoa" },
                    { 149, "SM", "San Marino" },
                    { 150, "ST", "Sao Tome and Principe" },
                    { 151, "SA", "Saudi Arabia" },
                    { 152, "SN", "Senegal" },
                    { 153, "RS", "Serbia" },
                    { 154, "SC", "Seychelles" },
                    { 155, "SL", "Sierra Leone" },
                    { 156, "SG", "Singapore" },
                    { 157, "SK", "Slovakia" },
                    { 158, "SI", "Slovenia" },
                    { 159, "SB", "Solomon Islands" },
                    { 160, "SO", "Somalia" },
                    { 161, "ZA", "South Africa" },
                    { 162, "SS", "South Sudan" },
                    { 163, "ES", "Spain" },
                    { 164, "LK", "Sri Lanka" },
                    { 165, "SD", "Sudan" },
                    { 166, "SR", "Suriname" },
                    { 167, "SE", "Sweden" },
                    { 168, "CH", "Switzerland" },
                    { 169, "SY", "Syria" },
                    { 170, "TW", "Taiwan" },
                    { 171, "TJ", "Tajikistan" },
                    { 172, "TZ", "Tanzania" },
                    { 173, "TH", "Thailand" },
                    { 174, "TL", "Timor-Leste" },
                    { 175, "TG", "Togo" },
                    { 176, "TO", "Tonga" },
                    { 177, "TT", "Trinidad and Tobago" },
                    { 178, "TN", "Tunisia" },
                    { 179, "TR", "Turkey" },
                    { 180, "TM", "Turkmenistan" },
                    { 181, "TV", "Tuvalu" },
                    { 182, "UG", "Uganda" },
                    { 183, "UA", "Ukraine" },
                    { 184, "AE", "United Arab Emirates" },
                    { 185, "GB", "United Kingdom" },
                    { 186, "US", "United States" },
                    { 187, "UY", "Uruguay" },
                    { 188, "UZ", "Uzbekistan" },
                    { 189, "VU", "Vanuatu" },
                    { 190, "VE", "Venezuela" },
                    { 191, "VN", "Vietnam" },
                    { 192, "YE", "Yemen" },
                    { 193, "ZM", "Zambia" },
                    { 194, "ZW", "Zimbabwe" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 143);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 144);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 153);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 154);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 155);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 156);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 157);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 158);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 159);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 160);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 161);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 162);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 163);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 164);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 165);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 166);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 167);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 168);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 169);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 170);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 171);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 172);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 173);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 174);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 175);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 176);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 177);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 178);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 179);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 180);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 181);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 182);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 183);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 184);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 185);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 186);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 187);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 188);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 189);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 190);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 191);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 192);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 193);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 194);

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "Name" },
                values: new object[] { "SA", "Saudi Arabia" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Name" },
                values: new object[] { "US", "United States" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "Name" },
                values: new object[] { "GB", "United Kingdom" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "Name" },
                values: new object[] { "AE", "United Arab Emirates" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Code", "Name" },
                values: new object[] { "CA", "Canada" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "Name" },
                values: new object[] { "DE", "Germany" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Code", "Name" },
                values: new object[] { "FR", "France" });

            migrationBuilder.UpdateData(
                schema: "Identity",
                table: "Countries",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Code", "Name" },
                values: new object[] { "JP", "Japan" });
        }
    }
}
