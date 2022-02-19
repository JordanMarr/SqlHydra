TRUNCATE TABLE DimGeography;

INSERT INTO DimGeography(GeographyKey, City, StateProvinceCode, StateProvinceName, CountryRegionCode, EnglishCountryRegionName, SpanishCountryRegionName, FrenchCountryRegionName, PostalCode, SalesTerritoryKey)
SELECT 1, N'Alexandria', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2015', 9 FROM DUAL UNION ALL
SELECT 2, N'Coffs Harbour', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2450', 9 FROM DUAL UNION ALL
SELECT 3, N'Darlinghurst', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2010', 9 FROM DUAL UNION ALL
SELECT 4, N'Goulburn', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2580', 9 FROM DUAL UNION ALL
SELECT 5, N'Lane Cove', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'1597', 9 FROM DUAL UNION ALL
SELECT 6, N'Lavender Bay', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2060', 9 FROM DUAL UNION ALL
SELECT 7, N'Malabar', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2036', 9 FROM DUAL UNION ALL
SELECT 8, N'Matraville', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2036', 9 FROM DUAL UNION ALL
SELECT 9, N'Milsons Point', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2061', 9 FROM DUAL UNION ALL
SELECT 10, N'Newcastle', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2300', 9 FROM DUAL UNION ALL
SELECT 11, N'North Ryde', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2113', 9 FROM DUAL UNION ALL
SELECT 12, N'North Sydney', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2055', 9 FROM DUAL UNION ALL
SELECT 13, N'Port Macquarie', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2444', 9 FROM DUAL UNION ALL
SELECT 14, N'Rhodes', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2138', 9 FROM DUAL UNION ALL
SELECT 15, N'Silverwater', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2264', 9 FROM DUAL UNION ALL
SELECT 16, N'Springwood', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2777', 9 FROM DUAL UNION ALL
SELECT 17, N'St. Leonards', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2065', 9 FROM DUAL UNION ALL
SELECT 18, N'Sydney', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'1002', 9 FROM DUAL UNION ALL
SELECT 19, N'Wollongong', N'NSW', N'New South Wales', N'AU', N'Australia', N'Australia', N'Australie', N'2500', 9 FROM DUAL UNION ALL
SELECT 20, N'Brisbane', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4000', 9 FROM DUAL UNION ALL
SELECT 21, N'Caloundra', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4551', 9 FROM DUAL UNION ALL
SELECT 22, N'East Brisbane', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4169', 9 FROM DUAL UNION ALL
SELECT 23, N'Gold Coast', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4217', 9 FROM DUAL UNION ALL
SELECT 24, N'Hawthorne', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4171', 9 FROM DUAL UNION ALL
SELECT 25, N'Hervey Bay', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4655', 9 FROM DUAL UNION ALL
SELECT 26, N'Rockhampton', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4700', 9 FROM DUAL UNION ALL
SELECT 27, N'Townsville', N'QLD', N'Queensland', N'AU', N'Australia', N'Australia', N'Australie', N'4810', 9 FROM DUAL UNION ALL
SELECT 28, N'Cloverdale', N'SA', N'South Australia', N'AU', N'Australia', N'Australia', N'Australie', N'6105', 9 FROM DUAL UNION ALL
SELECT 29, N'Findon', N'SA', N'South Australia', N'AU', N'Australia', N'Australia', N'Australie', N'5023', 9 FROM DUAL UNION ALL
SELECT 30, N'Perth', N'SA', N'South Australia', N'AU', N'Australia', N'Australia', N'Australie', N'6006', 9 FROM DUAL UNION ALL
SELECT 31, N'Hobart', N'TAS', N'Tasmania', N'AU', N'Australia', N'Australia', N'Australie', N'7001', 9 FROM DUAL UNION ALL
SELECT 32, N'Bendigo', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3550', 9 FROM DUAL UNION ALL
SELECT 33, N'Cranbourne', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3977', 9 FROM DUAL UNION ALL
SELECT 34, N'Geelong', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3220', 9 FROM DUAL UNION ALL
SELECT 35, N'Melbourne', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3000', 9 FROM DUAL UNION ALL
SELECT 36, N'Melton', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3337', 9 FROM DUAL UNION ALL
SELECT 37, N'Seaford', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3198', 9 FROM DUAL UNION ALL
SELECT 38, N'South Melbourne', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3205', 9 FROM DUAL UNION ALL
SELECT 39, N'Sunbury', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3429', 9 FROM DUAL UNION ALL
SELECT 40, N'Warrnambool', N'VIC', N'Victoria', N'AU', N'Australia', N'Australia', N'Australie', N'3280', 9 FROM DUAL UNION ALL
SELECT 41, N'Calgary', N'AB', N'Alberta', N'CA', N'Canada', N'Canada', N'Canada', N'T2P 2G8', 6 FROM DUAL UNION ALL
SELECT 42, N'Edmonton', N'AB', N'Alberta', N'CA', N'Canada', N'Canada', N'Canada', N'T5', 6 FROM DUAL UNION ALL
SELECT 43, N'Burnaby', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V3J 6Z3', 6 FROM DUAL UNION ALL
SELECT 44, N'Burnaby', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V5A 3A6', 6 FROM DUAL UNION ALL
SELECT 45, N'Burnaby', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V5A 4X1', 6 FROM DUAL UNION ALL
SELECT 46, N'Burnaby', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V5G 4S4', 6 FROM DUAL UNION ALL
SELECT 47, N'Burnaby', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V5G 4W1', 6 FROM DUAL UNION ALL
SELECT 48, N'Burnaby', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V5H 3Z7', 6 FROM DUAL UNION ALL
SELECT 49, N'Cliffside', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V8Y 1L1', 6 FROM DUAL UNION ALL
SELECT 50, N'Haney', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V2W 1W2', 6 FROM DUAL UNION ALL
SELECT 51, N'Langford', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V9', 6 FROM DUAL UNION ALL
SELECT 52, N'Langley', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V3A 4R2', 6 FROM DUAL UNION ALL
SELECT 53, N'Metchosin', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V9', 6 FROM DUAL UNION ALL
SELECT 54, N'N. Vancouver', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V7L 4J4', 6 FROM DUAL UNION ALL
SELECT 55, N'Newton', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V2L3W8', 6 FROM DUAL UNION ALL
SELECT 56, N'Newton', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V2M1N7', 6 FROM DUAL UNION ALL
SELECT 57, N'Newton', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V2M1P1', 6 FROM DUAL UNION ALL
SELECT 58, N'Newton', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V2M1S6', 6 FROM DUAL UNION ALL
SELECT 59, N'Oak Bay', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V8P', 6 FROM DUAL UNION ALL
SELECT 60, N'Port Hammond', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V6B 3P7', 6 FROM DUAL UNION ALL
SELECT 61, N'Richmond', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V6B 3P7', 6 FROM DUAL UNION ALL
SELECT 62, N'Royal Oak', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V8X', 6 FROM DUAL UNION ALL
SELECT 63, N'Shawnee', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V8Z 4N5', 6 FROM DUAL UNION ALL
SELECT 64, N'Shawnee', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V9B 2C3', 6 FROM DUAL UNION ALL
SELECT 65, N'Shawnee', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V9B 5T2', 6 FROM DUAL UNION ALL
SELECT 66, N'Sooke', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V0', 6 FROM DUAL UNION ALL
SELECT 67, N'Surrey', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V3T 4W3', 6 FROM DUAL UNION ALL
SELECT 68, N'Vancouver', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V7L 4J4', 6 FROM DUAL UNION ALL
SELECT 69, N'Victoria', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V8V', 6 FROM DUAL UNION ALL
SELECT 70, N'Westminster', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V3L 1E7', 6 FROM DUAL UNION ALL
SELECT 71, N'Westminster', N'BC', N'British Columbia', N'CA', N'Canada', N'Canada', N'Canada', N'V3L 1H4', 6 FROM DUAL UNION ALL
SELECT 72, N'Winnipeg', N'MB', N'Manitoba', N'CA', N'Canada', N'Canada', N'Canada', N'R3', 6 FROM DUAL UNION ALL
SELECT 73, N'Saint John', N'NB', N'Brunswick', N'CA', N'Canada', N'Canada', N'Canada', N'E2P 1E3', 6 FROM DUAL UNION ALL
SELECT 74, N'Aurora', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L4G 7N6', 6 FROM DUAL UNION ALL
SELECT 75, N'Barrie', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L4N', 6 FROM DUAL UNION ALL
SELECT 76, N'Brampton', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L6W 2T7', 6 FROM DUAL UNION ALL
SELECT 77, N'Chalk Riber', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'K0J 1J0', 6 FROM DUAL UNION ALL
SELECT 78, N'Etobicoke', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M9W 3P3', 6 FROM DUAL UNION ALL
SELECT 79, N'Kanata', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'K2L 1H5', 6 FROM DUAL UNION ALL
SELECT 80, N'Kingston', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'7L', 6 FROM DUAL UNION ALL
SELECT 81, N'Markham', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L3S 3K2', 6 FROM DUAL UNION ALL
SELECT 82, N'Mississauga', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L4W 5J3', 6 FROM DUAL UNION ALL
SELECT 83, N'Mississauga', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L5A 1H6', 6 FROM DUAL UNION ALL
SELECT 84, N'Mississauga', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L5B 3V4', 6 FROM DUAL UNION ALL
SELECT 85, N'Nepean', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'K2J 2W5', 6 FROM DUAL UNION ALL
SELECT 86, N'North York', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M4C 4K6', 6 FROM DUAL UNION ALL
SELECT 87, N'Ottawa', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'K4B 1S1', 6 FROM DUAL UNION ALL
SELECT 88, N'Ottawa', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'K4B 1S2', 6 FROM DUAL UNION ALL
SELECT 89, N'Ottawa', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'K4B 1S3', 6 FROM DUAL UNION ALL
SELECT 90, N'Ottawa', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'K4B 1T7', 6 FROM DUAL UNION ALL
SELECT 91, N'Richmond Hill', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'L4E 3M5', 6 FROM DUAL UNION ALL
SELECT 92, N'Scarborough', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M1V 4M2', 6 FROM DUAL UNION ALL
SELECT 93, N'Toronto', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M4B 1V4', 6 FROM DUAL UNION ALL
SELECT 94, N'Toronto', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M4B 1V5', 6 FROM DUAL UNION ALL
SELECT 95, N'Toronto', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M4B 1V6', 6 FROM DUAL UNION ALL
SELECT 96, N'Toronto', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M4B 1V7', 6 FROM DUAL UNION ALL
SELECT 97, N'Vancouver', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'V5T 1Y9', 6 FROM DUAL UNION ALL
SELECT 98, N'Waterloo', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'N2V', 6 FROM DUAL UNION ALL
SELECT 99, N'Weston', N'ON', N'Ontario', N'CA', N'Canada', N'Canada', N'Canada', N'M9V 4W3', 6 FROM DUAL UNION ALL
SELECT 100, N'Brossard', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'J4Z 1C5', 6 FROM DUAL UNION ALL
SELECT 101, N'Brossard', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'J4Z 1R4', 6 FROM DUAL UNION ALL
SELECT 102, N'Dorval', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'H9P 1H1', 6 FROM DUAL UNION ALL
SELECT 103, N'Hull', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'8Y', 6 FROM DUAL UNION ALL
SELECT 104, N'Montreal', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'H1Y 2H3', 6 FROM DUAL UNION ALL
SELECT 105, N'Montreal', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'H1Y 2H5', 6 FROM DUAL UNION ALL
SELECT 106, N'Montreal', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'H1Y 2H7', 6 FROM DUAL UNION ALL
SELECT 107, N'Montreal', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'H1Y 2H8', 6 FROM DUAL UNION ALL
SELECT 108, N'Outremont', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'H1Y 2G5', 6 FROM DUAL UNION ALL
SELECT 109, N'Pnot-Rouge', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'J1E 2T7', 6 FROM DUAL UNION ALL
SELECT 110, N'Quebec', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'G1R', 6 FROM DUAL UNION ALL
SELECT 111, N'Sainte-Foy', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'G1W', 6 FROM DUAL UNION ALL
SELECT 112, N'Sillery', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'G1T', 6 FROM DUAL UNION ALL
SELECT 113, N'Ville De''anjou', N'QC', N'Quebec', N'CA', N'Canada', N'Canada', N'Canada', N'J1G 2R3', 6 FROM DUAL UNION ALL
SELECT 114, N'Berlin', N'BB', N'Brandenburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'14197', 8 FROM DUAL UNION ALL
SELECT 115, N'Eilenburg', N'BB', N'Brandenburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'04838', 8 FROM DUAL UNION ALL
SELECT 116, N'Augsburg', N'BY', N'Bayern', N'DE', N'Germany', N'Alemania', N'Allemagne', N'86150', 8 FROM DUAL UNION ALL
SELECT 117, N'Erlangen', N'BY', N'Bayern', N'DE', N'Germany', N'Alemania', N'Allemagne', N'91054', 8 FROM DUAL UNION ALL
SELECT 118, N'Frankfurt', N'BY', N'Bayern', N'DE', N'Germany', N'Alemania', N'Allemagne', N'91480', 8 FROM DUAL UNION ALL
SELECT 119, N'Grevenbroich', N'BY', N'Bayern', N'DE', N'Germany', N'Alemania', N'Allemagne', N'41485', 8 FROM DUAL UNION ALL
SELECT 120, N'Hof', N'BY', N'Bayern', N'DE', N'Germany', N'Alemania', N'Allemagne', N'95010', 8 FROM DUAL UNION ALL
SELECT 121, N'Ingolstadt', N'BY', N'Bayern', N'DE', N'Germany', N'Alemania', N'Allemagne', N'85049', 8 FROM DUAL UNION ALL
SELECT 122, N'Bad Soden', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'65800', 8 FROM DUAL UNION ALL
SELECT 123, N'Berlin', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'12171', 8 FROM DUAL UNION ALL
SELECT 124, N'Berlin', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'13441', 8 FROM DUAL UNION ALL
SELECT 125, N'Berlin', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'14111', 8 FROM DUAL UNION ALL
SELECT 126, N'Berlin', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'14129', 8 FROM DUAL UNION ALL
SELECT 127, N'Darmstadt', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'64283', 8 FROM DUAL UNION ALL
SELECT 128, N'Dresden', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'01071', 8 FROM DUAL UNION ALL
SELECT 129, N'Duesseldorf', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'40434', 8 FROM DUAL UNION ALL
SELECT 130, N'Duesseldorf', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'40605', 8 FROM DUAL UNION ALL
SELECT 131, N'Frankfurt', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'60323', 8 FROM DUAL UNION ALL
SELECT 132, N'Hamburg', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'22001', 8 FROM DUAL UNION ALL
SELECT 133, N'Kassel', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'34117', 8 FROM DUAL UNION ALL
SELECT 134, N'München', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'80074', 8 FROM DUAL UNION ALL
SELECT 135, N'Salzgitter', N'HE', N'Hessen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'38231', 8 FROM DUAL UNION ALL
SELECT 136, N'Ascheim', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'86171', 8 FROM DUAL UNION ALL
SELECT 137, N'Augsburg', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'86171', 8 FROM DUAL UNION ALL
SELECT 138, N'Berlin', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'10210', 8 FROM DUAL UNION ALL
SELECT 139, N'Berlin', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'10791', 8 FROM DUAL UNION ALL
SELECT 140, N'Berlin', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'12311', 8 FROM DUAL UNION ALL
SELECT 141, N'Berlin', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'14111', 8 FROM DUAL UNION ALL
SELECT 142, N'Bonn', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'53001', 8 FROM DUAL UNION ALL
SELECT 143, N'Bonn', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'53131', 8 FROM DUAL UNION ALL
SELECT 144, N'Essen', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'45001', 8 FROM DUAL UNION ALL
SELECT 145, N'Frankfurt am Main', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'60082', 8 FROM DUAL UNION ALL
SELECT 146, N'Frankfurt am Main', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'60355', 8 FROM DUAL UNION ALL
SELECT 147, N'Hamburg', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'20354', 8 FROM DUAL UNION ALL
SELECT 148, N'Muehlheim', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'63151', 8 FROM DUAL UNION ALL
SELECT 149, N'Mühlheim', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'63151', 8 FROM DUAL UNION ALL
SELECT 150, N'München', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'80074', 8 FROM DUAL UNION ALL
SELECT 151, N'Paderborn', N'HH', N'Hamburg', N'DE', N'Germany', N'Alemania', N'Allemagne', N'33041', 8 FROM DUAL UNION ALL
SELECT 152, N'Berlin', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'10501', 8 FROM DUAL UNION ALL
SELECT 153, N'Berlin', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'14197', 8 FROM DUAL UNION ALL
SELECT 154, N'Bonn', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'53131', 8 FROM DUAL UNION ALL
SELECT 155, N'Bottrop', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'46236', 8 FROM DUAL UNION ALL
SELECT 156, N'Braunschweig', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'38001', 8 FROM DUAL UNION ALL
SELECT 157, N'Hannover', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'30601', 8 FROM DUAL UNION ALL
SELECT 158, N'Leipzig', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'04139', 8 FROM DUAL UNION ALL
SELECT 159, N'München', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'80074', 8 FROM DUAL UNION ALL
SELECT 160, N'Paderborn', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'33098', 8 FROM DUAL UNION ALL
SELECT 161, N'Solingen', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'42651', 8 FROM DUAL UNION ALL
SELECT 162, N'Werne', N'NW', N'Nordrhein-Westfalen', N'DE', N'Germany', N'Alemania', N'Allemagne', N'59368', 8 FROM DUAL UNION ALL
SELECT 163, N'Berlin', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'12171', 8 FROM DUAL UNION ALL
SELECT 164, N'Berlin', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'12311', 8 FROM DUAL UNION ALL
SELECT 165, N'Berlin', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'14197', 8 FROM DUAL UNION ALL
SELECT 166, N'Frankfurt am Main', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'60061', 8 FROM DUAL UNION ALL
SELECT 167, N'Frankfurt am Main', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'60075', 8 FROM DUAL UNION ALL
SELECT 168, N'Kiel', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'24044', 8 FROM DUAL UNION ALL
SELECT 169, N'München', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'48001', 8 FROM DUAL UNION ALL
SELECT 170, N'Münster', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'48001', 8 FROM DUAL UNION ALL
SELECT 171, N'Neunkirchen', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'66578', 8 FROM DUAL UNION ALL
SELECT 172, N'Offenbach', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'63009', 8 FROM DUAL UNION ALL
SELECT 173, N'Poing', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'66041', 8 FROM DUAL UNION ALL
SELECT 174, N'Saarbrücken', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'66001', 8 FROM DUAL UNION ALL
SELECT 175, N'Saarlouis', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'66740', 8 FROM DUAL UNION ALL
SELECT 176, N'Stuttgart', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'70452', 8 FROM DUAL UNION ALL
SELECT 177, N'Stuttgart', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'70511', 8 FROM DUAL UNION ALL
SELECT 178, N'Sulzbach Taunus', N'SL', N'Saarland', N'DE', N'Germany', N'Alemania', N'Allemagne', N'66272', 8 FROM DUAL UNION ALL
SELECT 179, N'Saint Ouen', N'17', N'Charente-Maritime', N'FR', N'France', N'Francia', N'France', N'17490', 7 FROM DUAL UNION ALL
SELECT 180, N'Colomiers', N'31', N'Garonne (Haute)', N'FR', N'France', N'Francia', N'France', N'31770', 7 FROM DUAL UNION ALL
SELECT 181, N'Aujan Mournede', N'32', N'Gers', N'FR', N'France', N'Francia', N'France', N'32300', 7 FROM DUAL UNION ALL
SELECT 182, N'Saint Ouen', N'41', N'Loir et Cher', N'FR', N'France', N'Francia', N'France', N'41100', 7 FROM DUAL UNION ALL
SELECT 183, N'Orleans', N'45', N'Loiret', N'FR', N'France', N'Francia', N'France', N'45000', 7 FROM DUAL UNION ALL
SELECT 184, N'Metz', N'57', N'Moselle', N'FR', N'France', N'Francia', N'France', N'57000', 7 FROM DUAL UNION ALL
SELECT 185, N'Croix', N'59', N'Nord', N'FR', N'France', N'Francia', N'France', N'59170', 7 FROM DUAL UNION ALL
SELECT 186, N'Dunkerque', N'59', N'Nord', N'FR', N'France', N'Francia', N'France', N'59140', 7 FROM DUAL UNION ALL
SELECT 187, N'Lille', N'59', N'Nord', N'FR', N'France', N'Francia', N'France', N'59000', 7 FROM DUAL UNION ALL
SELECT 188, N'Roncq', N'59', N'Nord', N'FR', N'France', N'Francia', N'France', N'59223', 7 FROM DUAL UNION ALL
SELECT 189, N'Roubaix', N'59', N'Nord', N'FR', N'France', N'Francia', N'France', N'59100', 7 FROM DUAL UNION ALL
SELECT 190, N'Villeneuve-d''Ascq', N'59', N'Nord', N'FR', N'France', N'Francia', N'France', N'59491', 7 FROM DUAL UNION ALL
SELECT 191, N'Boulogne-sur-Mer', N'62', N'Pas de Calais', N'FR', N'France', N'Francia', N'France', N'62200', 7 FROM DUAL UNION ALL
SELECT 192, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75002', 7 FROM DUAL UNION ALL
SELECT 193, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75003', 7 FROM DUAL UNION ALL
SELECT 194, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75005', 7 FROM DUAL UNION ALL
SELECT 195, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75006', 7 FROM DUAL UNION ALL
SELECT 196, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75007', 7 FROM DUAL UNION ALL
SELECT 197, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75008', 7 FROM DUAL UNION ALL
SELECT 198, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75009', 7 FROM DUAL UNION ALL
SELECT 199, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75010', 7 FROM DUAL UNION ALL
SELECT 200, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75012', 7 FROM DUAL UNION ALL
SELECT 201, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75013', 7 FROM DUAL UNION ALL
SELECT 202, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75016', 7 FROM DUAL UNION ALL
SELECT 203, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75017', 7 FROM DUAL UNION ALL
SELECT 204, N'Paris', N'75', N'Seine (Paris)', N'FR', N'France', N'Francia', N'France', N'75019', 7 FROM DUAL UNION ALL
SELECT 205, N'Lieusaint', N'77', N'Seine et Marne', N'FR', N'France', N'Francia', N'France', N'77127', 7 FROM DUAL UNION ALL
SELECT 206, N'Roissy en Brie', N'77', N'Seine et Marne', N'FR', N'France', N'Francia', N'France', N'77680', 7 FROM DUAL UNION ALL
SELECT 207, N'Chatou', N'78', N'Yveline', N'FR', N'France', N'Francia', N'France', N'78400', 7 FROM DUAL UNION ALL
SELECT 208, N'Saint Germain en Laye', N'78', N'Yveline', N'FR', N'France', N'Francia', N'France', N'78100', 7 FROM DUAL UNION ALL
SELECT 209, N'Versailles', N'78', N'Yveline', N'FR', N'France', N'Francia', N'France', N'78000', 7 FROM DUAL UNION ALL
SELECT 210, N'Saint Ouen', N'80', N'Somme', N'FR', N'France', N'Francia', N'France', N'80610', 7 FROM DUAL UNION ALL
SELECT 211, N'Les Ulis', N'91', N'Essonne', N'FR', N'France', N'Francia', N'France', N'91940', 7 FROM DUAL UNION ALL
SELECT 212, N'Morangis', N'91', N'Essonne', N'FR', N'France', N'Francia', N'France', N'91420', 7 FROM DUAL UNION ALL
SELECT 213, N'Verrieres Le Buisson', N'91', N'Essonne', N'FR', N'France', N'Francia', N'France', N'91370', 7 FROM DUAL UNION ALL
SELECT 214, N'Boulogne-Billancourt', N'92', N'Hauts de Seine', N'FR', N'France', N'Francia', N'France', N'92100', 7 FROM DUAL UNION ALL
SELECT 215, N'Colombes', N'92', N'Hauts de Seine', N'FR', N'France', N'Francia', N'France', N'92700', 7 FROM DUAL UNION ALL
SELECT 216, N'Courbevoie', N'92', N'Hauts de Seine', N'FR', N'France', N'Francia', N'France', N'92400', 7 FROM DUAL UNION ALL
SELECT 217, N'Paris La Defense', N'92', N'Hauts de Seine', N'FR', N'France', N'Francia', N'France', N'92081', 7 FROM DUAL UNION ALL
SELECT 218, N'Sèvres', N'92', N'Hauts de Seine', N'FR', N'France', N'Francia', N'France', N'92310', 7 FROM DUAL UNION ALL
SELECT 219, N'Suresnes', N'92', N'Hauts de Seine', N'FR', N'France', N'Francia', N'France', N'92150', 7 FROM DUAL UNION ALL
SELECT 220, N'Bobigny', N'93', N'Seine Saint Denis', N'FR', N'France', N'Francia', N'France', N'93000', 7 FROM DUAL UNION ALL
SELECT 221, N'Drancy', N'93', N'Seine Saint Denis', N'FR', N'France', N'Francia', N'France', N'93700', 7 FROM DUAL UNION ALL
SELECT 222, N'Pantin', N'93', N'Seine Saint Denis', N'FR', N'France', N'Francia', N'France', N'93500', 7 FROM DUAL UNION ALL
SELECT 223, N'Saint-Denis', N'93', N'Seine Saint Denis', N'FR', N'France', N'Francia', N'France', N'93400', 7 FROM DUAL UNION ALL
SELECT 224, N'Tremblay-en-France', N'93', N'Seine Saint Denis', N'FR', N'France', N'Francia', N'France', N'93290', 7 FROM DUAL UNION ALL
SELECT 225, N'Orly', N'94', N'Val de Marne', N'FR', N'France', N'Francia', N'France', N'94310', 7 FROM DUAL UNION ALL
SELECT 226, N'Cergy', N'95', N'Val d''Oise', N'FR', N'France', N'Francia', N'France', N'95000', 7 FROM DUAL UNION ALL
SELECT 227, N'Abingdon', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'OX14 4SE', 10 FROM DUAL UNION ALL
SELECT 228, N'Basingstoke Hants', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'RG24 8PL', 10 FROM DUAL UNION ALL
SELECT 229, N'Berks', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SL4 1RH', 10 FROM DUAL UNION ALL
SELECT 230, N'Berkshire', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'RG11 5TP', 10 FROM DUAL UNION ALL
SELECT 231, N'Billericay', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'CM11', 10 FROM DUAL UNION ALL
SELECT 232, N'Birmingham', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'B29 6SL', 10 FROM DUAL UNION ALL
SELECT 233, N'Bracknell', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'RG12 8TB', 10 FROM DUAL UNION ALL
SELECT 234, N'Bury', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'PE17', 10 FROM DUAL UNION ALL
SELECT 235, N'Cambridge', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'CB4 4BZ', 10 FROM DUAL UNION ALL
SELECT 236, N'Cheltenham', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'GL50', 10 FROM DUAL UNION ALL
SELECT 237, N'Esher-Molesey', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'EM15', 10 FROM DUAL UNION ALL
SELECT 238, N'Gateshead', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'GA10', 10 FROM DUAL UNION ALL
SELECT 239, N'Gloucestershire', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'GL7 1RY', 10 FROM DUAL UNION ALL
SELECT 240, N'High Wycombe', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'HP10 9QY', 10 FROM DUAL UNION ALL
SELECT 241, N'Kirkby', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'KB9', 10 FROM DUAL UNION ALL
SELECT 242, N'Lancaster', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'LA1 1LN', 10 FROM DUAL UNION ALL
SELECT 243, N'Leeds', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'LE18', 10 FROM DUAL UNION ALL
SELECT 244, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'C2H 7AU', 10 FROM DUAL UNION ALL
SELECT 245, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'E17 6JF', 10 FROM DUAL UNION ALL
SELECT 246, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'EC1R 0DU', 10 FROM DUAL UNION ALL
SELECT 247, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SE1 8HL', 10 FROM DUAL UNION ALL
SELECT 248, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SW19 3RU', 10 FROM DUAL UNION ALL
SELECT 249, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SW1P 2NU', 10 FROM DUAL UNION ALL
SELECT 250, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SW6 SBY', 10 FROM DUAL UNION ALL
SELECT 251, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SW8 1XD', 10 FROM DUAL UNION ALL
SELECT 252, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SW8 4BG', 10 FROM DUAL UNION ALL
SELECT 253, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'W10 6BL', 10 FROM DUAL UNION ALL
SELECT 254, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'W1N 9FA', 10 FROM DUAL UNION ALL
SELECT 255, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'W1V 5RN', 10 FROM DUAL UNION ALL
SELECT 256, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'W1X3SE', 10 FROM DUAL UNION ALL
SELECT 257, N'London', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'W1Y 3RA', 10 FROM DUAL UNION ALL
SELECT 258, N'Maidenhead', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'SL67RJ', 10 FROM DUAL UNION ALL
SELECT 259, N'Milton Keynes', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'MK8 8DF', 10 FROM DUAL UNION ALL
SELECT 260, N'Milton Keynes', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'MK8 8ZD', 10 FROM DUAL UNION ALL
SELECT 261, N'Newcastle upon Tyne', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'NT20', 10 FROM DUAL UNION ALL
SELECT 262, N'Oxford', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'OX1', 10 FROM DUAL UNION ALL
SELECT 263, N'Oxford', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'OX14 4SE', 10 FROM DUAL UNION ALL
SELECT 264, N'Oxon', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'OX16 8RS', 10 FROM DUAL UNION ALL
SELECT 265, N'Peterborough', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'PB12', 10 FROM DUAL UNION ALL
SELECT 266, N'Reading', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'RG7 5H7', 10 FROM DUAL UNION ALL
SELECT 267, N'Runcorn', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'TY31', 10 FROM DUAL UNION ALL
SELECT 268, N'Liverpool', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'L4 4HB', 10 FROM DUAL UNION ALL
SELECT 269, N'Stoke-on-Trent', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'AS23', 10 FROM DUAL UNION ALL
SELECT 270, N'W. York', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'BD1 4SJ', 10 FROM DUAL UNION ALL
SELECT 271, N'Warrington', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'WA1', 10 FROM DUAL UNION ALL
SELECT 272, N'Warrington', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'WA3 7BH', 10 FROM DUAL UNION ALL
SELECT 273, N'Watford', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'WA3', 10 FROM DUAL UNION ALL
SELECT 274, N'West Sussex', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'RH15 9UD', 10 FROM DUAL UNION ALL
SELECT 275, N'Wokingham', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'RG41 1QW', 10 FROM DUAL UNION ALL
SELECT 276, N'Woolston', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'WA1 4SY', 10 FROM DUAL UNION ALL
SELECT 277, N'York', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'Y024 1GF', 10 FROM DUAL UNION ALL
SELECT 278, N'York', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'Y03 4TN', 10 FROM DUAL UNION ALL
SELECT 279, N'York', N'ENG', N'England', N'GB', N'United Kingdom', N'Reino Unido', N'Royaume-Uni', N'YO15', 10 FROM DUAL UNION ALL
SELECT 280, N'Birmingham', N'AL', N'Alabama', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'35203', 5 FROM DUAL UNION ALL
SELECT 281, N'Florence', N'AL', N'Alabama', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'35630', 5 FROM DUAL UNION ALL
SELECT 282, N'Huntsville', N'AL', N'Alabama', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'35801', 5 FROM DUAL UNION ALL
SELECT 283, N'Mobile', N'AL', N'Alabama', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'36602', 5 FROM DUAL UNION ALL
SELECT 284, N'Montgomery', N'AL', N'Alabama', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'36104', 5 FROM DUAL UNION ALL
SELECT 285, N'Chandler', N'AZ', N'Arizona', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'85225', 4 FROM DUAL UNION ALL
SELECT 286, N'Gilbert', N'AZ', N'Arizona', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'85233', 4 FROM DUAL UNION ALL
SELECT 287, N'Mesa', N'AZ', N'Arizona', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'85201', 4 FROM DUAL UNION ALL
SELECT 288, N'Phoenix', N'AZ', N'Arizona', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'85004', 4 FROM DUAL UNION ALL
SELECT 289, N'Scottsdale', N'AZ', N'Arizona', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'85257', 4 FROM DUAL UNION ALL
SELECT 290, N'Surprise', N'AZ', N'Arizona', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'85374', 4 FROM DUAL UNION ALL
SELECT 291, N'Tucson', N'AZ', N'Arizona', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'85701', 4 FROM DUAL UNION ALL
SELECT 292, N'Alhambra', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91801', 4 FROM DUAL UNION ALL
SELECT 293, N'Alpine', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91901', 4 FROM DUAL UNION ALL
SELECT 294, N'Auburn', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95603', 4 FROM DUAL UNION ALL
SELECT 295, N'Baldwin Park', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91706', 4 FROM DUAL UNION ALL
SELECT 296, N'Barstow', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92311', 4 FROM DUAL UNION ALL
SELECT 297, N'Bell Gardens', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90201', 4 FROM DUAL UNION ALL
SELECT 298, N'Bellflower', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90706', 4 FROM DUAL UNION ALL
SELECT 299, N'Berkeley', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94704', 4 FROM DUAL UNION ALL
SELECT 300, N'Beverly Hills', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90210', 4 FROM DUAL UNION ALL
SELECT 301, N'Burbank', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91502', 4 FROM DUAL UNION ALL
SELECT 302, N'Burlingame', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94010', 4 FROM DUAL UNION ALL
SELECT 303, N'Camarillo', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'93010', 4 FROM DUAL UNION ALL
SELECT 304, N'Canoga Park', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91303', 4 FROM DUAL UNION ALL
SELECT 305, N'Carson', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90746', 4 FROM DUAL UNION ALL
SELECT 306, N'Cerritos', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90703', 4 FROM DUAL UNION ALL
SELECT 307, N'Chula Vista', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91910', 4 FROM DUAL UNION ALL
SELECT 308, N'Citrus Heights', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95610', 4 FROM DUAL UNION ALL
SELECT 309, N'City Of Commerce', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90040', 4 FROM DUAL UNION ALL
SELECT 310, N'Colma', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94014', 4 FROM DUAL UNION ALL
SELECT 311, N'Concord', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94519', 4 FROM DUAL UNION ALL
SELECT 312, N'Coronado', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92118', 4 FROM DUAL UNION ALL
SELECT 313, N'Culver City', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90232', 4 FROM DUAL UNION ALL
SELECT 314, N'Daly City', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94015', 4 FROM DUAL UNION ALL
SELECT 315, N'Downey', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90241', 4 FROM DUAL UNION ALL
SELECT 316, N'El Cajon', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92020', 4 FROM DUAL UNION ALL
SELECT 317, N'El Segundo', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90245', 4 FROM DUAL UNION ALL
SELECT 318, N'Elk Grove', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95624', 4 FROM DUAL UNION ALL
SELECT 319, N'Escondido', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92025', 4 FROM DUAL UNION ALL
SELECT 320, N'Eureka', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95501', 4 FROM DUAL UNION ALL
SELECT 321, N'Fontana', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92335', 4 FROM DUAL UNION ALL
SELECT 322, N'Fremont', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94536', 4 FROM DUAL UNION ALL
SELECT 323, N'Fullerton', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92831', 4 FROM DUAL UNION ALL
SELECT 324, N'Gilroy', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95020', 4 FROM DUAL UNION ALL
SELECT 325, N'Glendale', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91203', 4 FROM DUAL UNION ALL
SELECT 326, N'Grossmont', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91941', 4 FROM DUAL UNION ALL
SELECT 327, N'Hanford', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'93230', 4 FROM DUAL UNION ALL
SELECT 328, N'Hayward', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94541', 4 FROM DUAL UNION ALL
SELECT 329, N'Imperial Beach', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91932', 4 FROM DUAL UNION ALL
SELECT 330, N'Irvine', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92614', 4 FROM DUAL UNION ALL
SELECT 331, N'La Jolla', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92806', 4 FROM DUAL UNION ALL
SELECT 332, N'La Mesa', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91941', 4 FROM DUAL UNION ALL
SELECT 333, N'Lake Elsinore', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92530', 4 FROM DUAL UNION ALL
SELECT 334, N'Lakewood', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90712', 4 FROM DUAL UNION ALL
SELECT 335, N'Lemon Grove', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91945', 4 FROM DUAL UNION ALL
SELECT 336, N'Lincoln Acres', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91950', 4 FROM DUAL UNION ALL
SELECT 337, N'Long Beach', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90802', 4 FROM DUAL UNION ALL
SELECT 338, N'Los Angeles', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90012', 4 FROM DUAL UNION ALL
SELECT 339, N'Mill Valley', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94941', 4 FROM DUAL UNION ALL
SELECT 340, N'Milpitas', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95035', 4 FROM DUAL UNION ALL
SELECT 341, N'Modesto', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95354', 4 FROM DUAL UNION ALL
SELECT 342, N'Monrovia', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91016', 4 FROM DUAL UNION ALL
SELECT 343, N'National City', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91950', 4 FROM DUAL UNION ALL
SELECT 344, N'Newark', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94560', 4 FROM DUAL UNION ALL
SELECT 345, N'Newport Beach', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92625', 4 FROM DUAL UNION ALL
SELECT 346, N'Norwalk', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90650', 4 FROM DUAL UNION ALL
SELECT 347, N'Novato', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94947', 4 FROM DUAL UNION ALL
SELECT 348, N'Oakland', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94611', 4 FROM DUAL UNION ALL
SELECT 349, N'Ontario', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91764', 4 FROM DUAL UNION ALL
SELECT 350, N'Orange', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92867', 4 FROM DUAL UNION ALL
SELECT 351, N'Oxnard', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'93030', 4 FROM DUAL UNION ALL
SELECT 352, N'Palo Alto', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94303', 4 FROM DUAL UNION ALL
SELECT 353, N'Pleasanton', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94566', 4 FROM DUAL UNION ALL
SELECT 354, N'Redlands', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92373', 4 FROM DUAL UNION ALL
SELECT 355, N'Redwood City', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94063', 4 FROM DUAL UNION ALL
SELECT 356, N'Sacramento', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95814', 4 FROM DUAL UNION ALL
SELECT 357, N'San Bruno', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94066', 4 FROM DUAL UNION ALL
SELECT 358, N'San Carlos', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94070', 4 FROM DUAL UNION ALL
SELECT 359, N'San Diego', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92102', 4 FROM DUAL UNION ALL
SELECT 360, N'San Francisco', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94109', 4 FROM DUAL UNION ALL
SELECT 361, N'San Gabriel', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91776', 4 FROM DUAL UNION ALL
SELECT 362, N'San Jose', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95112', 4 FROM DUAL UNION ALL
SELECT 363, N'San Mateo', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94404', 4 FROM DUAL UNION ALL
SELECT 364, N'San Ramon', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94583', 4 FROM DUAL UNION ALL
SELECT 365, N'San Ysidro', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92173', 4 FROM DUAL UNION ALL
SELECT 366, N'Sand City', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'93955', 4 FROM DUAL UNION ALL
SELECT 367, N'Santa Ana', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92701', 4 FROM DUAL UNION ALL
SELECT 368, N'Santa Cruz', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95062', 4 FROM DUAL UNION ALL
SELECT 369, N'Santa Monica', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90401', 4 FROM DUAL UNION ALL
SELECT 370, N'Sherman Oaks', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91403', 4 FROM DUAL UNION ALL
SELECT 371, N'Simi Valley', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'93065', 4 FROM DUAL UNION ALL
SELECT 372, N'Spring Valley', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91977', 4 FROM DUAL UNION ALL
SELECT 373, N'Stockton', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95202', 4 FROM DUAL UNION ALL
SELECT 374, N'Torrance', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90505', 4 FROM DUAL UNION ALL
SELECT 375, N'Trabuco Canyon', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92679', 4 FROM DUAL UNION ALL
SELECT 376, N'Union City', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94587', 4 FROM DUAL UNION ALL
SELECT 377, N'Upland', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91786', 4 FROM DUAL UNION ALL
SELECT 378, N'Vacaville', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'95688', 4 FROM DUAL UNION ALL
SELECT 379, N'Van Nuys', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91411', 4 FROM DUAL UNION ALL
SELECT 380, N'Visalia', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'93291', 4 FROM DUAL UNION ALL
SELECT 381, N'Vista', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'92084', 4 FROM DUAL UNION ALL
SELECT 382, N'Walnut Creek', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'94596', 4 FROM DUAL UNION ALL
SELECT 383, N'West Covina', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91791', 4 FROM DUAL UNION ALL
SELECT 384, N'Whittier', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'90605', 4 FROM DUAL UNION ALL
SELECT 385, N'Woodland Hills', N'CA', N'California', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'91364', 4 FROM DUAL UNION ALL
SELECT 386, N'Denver', N'CO', N'Colorado', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'80203', 3 FROM DUAL UNION ALL
SELECT 387, N'Englewood', N'CO', N'Colorado', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'80110', 3 FROM DUAL UNION ALL
SELECT 388, N'Greeley', N'CO', N'Colorado', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'80631', 3 FROM DUAL UNION ALL
SELECT 389, N'Longmont', N'CO', N'Colorado', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'80501', 3 FROM DUAL UNION ALL
SELECT 390, N'Loveland', N'CO', N'Colorado', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'80537', 3 FROM DUAL UNION ALL
SELECT 391, N'Parker', N'CO', N'Colorado', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'80138', 3 FROM DUAL UNION ALL
SELECT 392, N'Westminster', N'CO', N'Colorado', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'80030', 3 FROM DUAL UNION ALL
SELECT 393, N'East Haven', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06512', 2 FROM DUAL UNION ALL
SELECT 394, N'Farmington', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06032', 2 FROM DUAL UNION ALL
SELECT 395, N'Hamden', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06518', 2 FROM DUAL UNION ALL
SELECT 396, N'Milford', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06460', 2 FROM DUAL UNION ALL
SELECT 397, N'New Haven', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06510', 2 FROM DUAL UNION ALL
SELECT 398, N'Stamford', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06901', 2 FROM DUAL UNION ALL
SELECT 399, N'Waterbury', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06710', 2 FROM DUAL UNION ALL
SELECT 400, N'Westport', N'CT', N'Connecticut', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'06880', 2 FROM DUAL UNION ALL
SELECT 401, N'Altamonte Springs', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'32701', 5 FROM DUAL UNION ALL
SELECT 402, N'Bradenton', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'34205', 5 FROM DUAL UNION ALL
SELECT 403, N'Clearwater', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33755', 5 FROM DUAL UNION ALL
SELECT 404, N'Destin', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'32541', 5 FROM DUAL UNION ALL
SELECT 405, N'Hollywood', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33021', 5 FROM DUAL UNION ALL
SELECT 406, N'Kendall', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33143', 5 FROM DUAL UNION ALL
SELECT 407, N'Lakeland', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33801', 5 FROM DUAL UNION ALL
SELECT 408, N'Merritt Island', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'32952', 5 FROM DUAL UNION ALL
SELECT 409, N'Miami', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33127', 5 FROM DUAL UNION ALL
SELECT 410, N'North Miami Beach', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33162', 5 FROM DUAL UNION ALL
SELECT 411, N'Orlando', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'32804', 5 FROM DUAL UNION ALL
SELECT 412, N'Sarasota', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'34236', 5 FROM DUAL UNION ALL
SELECT 413, N'Sunrise', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33322', 5 FROM DUAL UNION ALL
SELECT 414, N'Tampa', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'33602', 5 FROM DUAL UNION ALL
SELECT 415, N'Vero Beach', N'FL', N'Florida', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'32960', 5 FROM DUAL UNION ALL
SELECT 416, N'Atlanta', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30308', 5 FROM DUAL UNION ALL
SELECT 417, N'Augusta', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30901', 5 FROM DUAL UNION ALL
SELECT 418, N'Austell', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30106', 5 FROM DUAL UNION ALL
SELECT 419, N'Byron', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'31008', 5 FROM DUAL UNION ALL
SELECT 420, N'Clarkston', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30021', 5 FROM DUAL UNION ALL
SELECT 421, N'Columbus', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'31901', 5 FROM DUAL UNION ALL
SELECT 422, N'Decatur', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30030', 5 FROM DUAL UNION ALL
SELECT 423, N'La Grange', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30240', 5 FROM DUAL UNION ALL
SELECT 424, N'Marietta', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30060', 5 FROM DUAL UNION ALL
SELECT 425, N'Mcdonough', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30253', 5 FROM DUAL UNION ALL
SELECT 426, N'Savannah', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'31401', 5 FROM DUAL UNION ALL
SELECT 427, N'Suwanee', N'GA', N'Georgia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'30024', 5 FROM DUAL UNION ALL
SELECT 428, N'Idaho Falls', N'ID', N'Idaho', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'83402', 1 FROM DUAL UNION ALL
SELECT 429, N'Lewiston', N'ID', N'Idaho', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'83501', 1 FROM DUAL UNION ALL
SELECT 430, N'Sandpoint', N'ID', N'Idaho', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'83864', 1 FROM DUAL UNION ALL
SELECT 431, N'Carol Stream', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'60188', 3 FROM DUAL UNION ALL
SELECT 432, N'Chicago', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'60610', 3 FROM DUAL UNION ALL
SELECT 433, N'Elgin', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'60120', 3 FROM DUAL UNION ALL
SELECT 434, N'Joliet', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'60433', 3 FROM DUAL UNION ALL
SELECT 435, N'Moline', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'61265', 3 FROM DUAL UNION ALL
SELECT 436, N'Norridge', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'60706', 3 FROM DUAL UNION ALL
SELECT 437, N'Peoria', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'61606', 3 FROM DUAL UNION ALL
SELECT 438, N'Tuscola', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'61953', 3 FROM DUAL UNION ALL
SELECT 439, N'West Chicago', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'60185', 3 FROM DUAL UNION ALL
SELECT 440, N'Wood Dale', N'IL', N'Illinois', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'60191', 3 FROM DUAL UNION ALL
SELECT 441, N'Daleville', N'IN', N'Indiana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'47334', 2 FROM DUAL UNION ALL
SELECT 442, N'Fort Wayne', N'IN', N'Indiana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'46807', 2 FROM DUAL UNION ALL
SELECT 443, N'Indianapolis', N'IN', N'Indiana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'46204', 2 FROM DUAL UNION ALL
SELECT 444, N'Logansport', N'IN', N'Indiana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'46947', 2 FROM DUAL UNION ALL
SELECT 445, N'Michigan City', N'IN', N'Indiana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'46360', 2 FROM DUAL UNION ALL
SELECT 446, N'New Castle', N'IN', N'Indiana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'47362', 2 FROM DUAL UNION ALL
SELECT 447, N'South Bend', N'IN', N'Indiana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'46601', 2 FROM DUAL UNION ALL
SELECT 448, N'Campbellsville', N'KY', N'Kentucky', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'42718', 5 FROM DUAL UNION ALL
SELECT 449, N'Florence', N'KY', N'Kentucky', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'41042', 5 FROM DUAL UNION ALL
SELECT 450, N'Newport', N'KY', N'Kentucky', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'41071', 5 FROM DUAL UNION ALL
SELECT 451, N'Saint Matthews', N'KY', N'Kentucky', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'40207', 5 FROM DUAL UNION ALL
SELECT 452, N'Somerset', N'KY', N'Kentucky', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'42501', 5 FROM DUAL UNION ALL
SELECT 453, N'Braintree', N'MA', N'Massachusetts', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'02184', 2 FROM DUAL UNION ALL
SELECT 454, N'Norwood', N'MA', N'Massachusetts', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'02062', 2 FROM DUAL UNION ALL
SELECT 455, N'Randolph', N'MA', N'Massachusetts', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'02368', 2 FROM DUAL UNION ALL
SELECT 456, N'Saugus', N'MA', N'Massachusetts', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'01906', 2 FROM DUAL UNION ALL
SELECT 457, N'Wrentham', N'MA', N'Massachusetts', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'02093', 2 FROM DUAL UNION ALL
SELECT 458, N'Baltimore', N'MD', N'Maryland', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'21201', 2 FROM DUAL UNION ALL
SELECT 459, N'Kittery', N'ME', N'Maine', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'03904', 2 FROM DUAL UNION ALL
SELECT 460, N'Detroit', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48226', 3 FROM DUAL UNION ALL
SELECT 461, N'Holland', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'49423', 3 FROM DUAL UNION ALL
SELECT 462, N'Howell', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48843', 3 FROM DUAL UNION ALL
SELECT 463, N'Madison Heights', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48071', 3 FROM DUAL UNION ALL
SELECT 464, N'Midland', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48640', 3 FROM DUAL UNION ALL
SELECT 465, N'Monroe', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98272', 3 FROM DUAL UNION ALL
SELECT 466, N'Novi', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48375', 3 FROM DUAL UNION ALL
SELECT 467, N'Pontiac', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48342', 3 FROM DUAL UNION ALL
SELECT 468, N'Port Huron', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48060', 3 FROM DUAL UNION ALL
SELECT 469, N'Redford', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48239', 3 FROM DUAL UNION ALL
SELECT 470, N'Saginaw', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48601', 3 FROM DUAL UNION ALL
SELECT 471, N'Southfield', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48034', 3 FROM DUAL UNION ALL
SELECT 472, N'Southgate', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48195', 3 FROM DUAL UNION ALL
SELECT 473, N'Westland', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'48185', 3 FROM DUAL UNION ALL
SELECT 474, N'Zeeland', N'MI', N'Michigan', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'49464', 3 FROM DUAL UNION ALL
SELECT 475, N'Branch', N'MN', N'Minnesota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'55056', 3 FROM DUAL UNION ALL
SELECT 476, N'Duluth', N'MN', N'Minnesota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'55802', 3 FROM DUAL UNION ALL
SELECT 477, N'Edina', N'MN', N'Minnesota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'55436', 3 FROM DUAL UNION ALL
SELECT 478, N'Medford', N'MN', N'Minnesota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'55049', 3 FROM DUAL UNION ALL
SELECT 479, N'Minneapolis', N'MN', N'Minnesota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'55402', 3 FROM DUAL UNION ALL
SELECT 480, N'Woodbury', N'MN', N'Minnesota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'55125', 3 FROM DUAL UNION ALL
SELECT 481, N'Branson', N'MO', N'Missouri', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'65616', 3 FROM DUAL UNION ALL
SELECT 482, N'Ferguson', N'MO', N'Missouri', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'63135', 3 FROM DUAL UNION ALL
SELECT 483, N'Jefferson City', N'MO', N'Missouri', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'65101', 3 FROM DUAL UNION ALL
SELECT 484, N'Kansas City', N'MO', N'Missouri', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'64106', 3 FROM DUAL UNION ALL
SELECT 485, N'Odessa', N'MO', N'Missouri', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'64076', 3 FROM DUAL UNION ALL
SELECT 486, N'Saint Ann', N'MO', N'Missouri', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'63074', 3 FROM DUAL UNION ALL
SELECT 487, N'Saint Louis', N'MO', N'Missouri', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'63103', 3 FROM DUAL UNION ALL
SELECT 488, N'Biloxi', N'MS', N'Mississippi', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'39530', 5 FROM DUAL UNION ALL
SELECT 489, N'Gulfport', N'MS', N'Mississippi', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'39501', 5 FROM DUAL UNION ALL
SELECT 490, N'Tupelo', N'MS', N'Mississippi', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'38804', 5 FROM DUAL UNION ALL
SELECT 491, N'Billings', N'MT', N'Montana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'59101', 1 FROM DUAL UNION ALL
SELECT 492, N'Great Falls', N'MT', N'Montana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'59401', 1 FROM DUAL UNION ALL
SELECT 493, N'Missoula', N'MT', N'Montana', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'59801', 1 FROM DUAL UNION ALL
SELECT 494, N'Charlotte', N'NC', N'North Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'28202', 5 FROM DUAL UNION ALL
SELECT 495, N'Greensboro', N'NC', N'North Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'27412', 5 FROM DUAL UNION ALL
SELECT 496, N'Kannapolis', N'NC', N'North Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'28081', 5 FROM DUAL UNION ALL
SELECT 497, N'Raleigh', N'NC', N'North Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'27603', 5 FROM DUAL UNION ALL
SELECT 498, N'Rocky Mount', N'NC', N'North Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'27803', 5 FROM DUAL UNION ALL
SELECT 499, N'Smithfield', N'NC', N'North Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'27577', 5 FROM DUAL UNION ALL
SELECT 500, N'Winston-Salem', N'NC', N'North Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'27104', 5 FROM DUAL UNION ALL
SELECT 501, N'Hooksett', N'NH', N'New Hampshire', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'03106', 2 FROM DUAL UNION ALL
SELECT 502, N'Nashua', N'NH', N'New Hampshire', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'03064', 2 FROM DUAL UNION ALL
SELECT 503, N'Plaistow', N'NH', N'New Hampshire', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'03865', 2 FROM DUAL UNION ALL
SELECT 504, N'Tilton', N'NH', N'New Hampshire', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'03276', 2 FROM DUAL UNION ALL
SELECT 505, N'Las Cruces', N'NM', N'New Mexico', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'88001', 4 FROM DUAL UNION ALL
SELECT 506, N'Rio Rancho', N'NM', N'New Mexico', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'87124', 4 FROM DUAL UNION ALL
SELECT 507, N'Santa Fe', N'NM', N'New Mexico', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'87501', 4 FROM DUAL UNION ALL
SELECT 508, N'Fernley', N'NV', N'Nevada', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'89408', 1 FROM DUAL UNION ALL
SELECT 509, N'Las Vegas', N'NV', N'Nevada', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'89106', 1 FROM DUAL UNION ALL
SELECT 510, N'North Las Vegas', N'NV', N'Nevada', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'89030', 1 FROM DUAL UNION ALL
SELECT 511, N'Reno', N'NV', N'Nevada', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'89502', 1 FROM DUAL UNION ALL
SELECT 512, N'Sparks', N'NV', N'Nevada', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'89431', 1 FROM DUAL UNION ALL
SELECT 513, N'Central Valley', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'10917', 2 FROM DUAL UNION ALL
SELECT 514, N'Cheektowaga', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'14227', 2 FROM DUAL UNION ALL
SELECT 515, N'Clay', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'13041', 2 FROM DUAL UNION ALL
SELECT 516, N'De Witt', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'13214', 2 FROM DUAL UNION ALL
SELECT 517, N'Endicott', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'13760', 2 FROM DUAL UNION ALL
SELECT 518, N'Ithaca', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'14850', 2 FROM DUAL UNION ALL
SELECT 519, N'Lake George', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'12845', 2 FROM DUAL UNION ALL
SELECT 520, N'Melville', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'11747', 2 FROM DUAL UNION ALL
SELECT 521, N'New Hartford', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'13413', 2 FROM DUAL UNION ALL
SELECT 522, N'New York', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'10007', 2 FROM DUAL UNION ALL
SELECT 523, N'Valley Stream', N'NY', N'New York', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'11580', 2 FROM DUAL UNION ALL
SELECT 524, N'Burbank', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'44214', 2 FROM DUAL UNION ALL
SELECT 525, N'Cincinnati', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'45202', 2 FROM DUAL UNION ALL
SELECT 526, N'Columbus', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'43215', 2 FROM DUAL UNION ALL
SELECT 527, N'Euclid', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'44119', 2 FROM DUAL UNION ALL
SELECT 528, N'Heath', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'43056', 2 FROM DUAL UNION ALL
SELECT 529, N'Holland', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'43528', 2 FROM DUAL UNION ALL
SELECT 530, N'Mansfield', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'44903', 2 FROM DUAL UNION ALL
SELECT 531, N'Mentor', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'44060', 2 FROM DUAL UNION ALL
SELECT 532, N'North Randall', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'44128', 2 FROM DUAL UNION ALL
SELECT 533, N'Oberlin', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'44074', 2 FROM DUAL UNION ALL
SELECT 534, N'Springdale', N'OH', N'Ohio', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'45246', 2 FROM DUAL UNION ALL
SELECT 535, N'Albany', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97321', 1 FROM DUAL UNION ALL
SELECT 536, N'Beaverton', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97005', 1 FROM DUAL UNION ALL
SELECT 537, N'Clackamas', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97015', 1 FROM DUAL UNION ALL
SELECT 538, N'Clackamas', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97015-6403', 1 FROM DUAL UNION ALL
SELECT 539, N'Corvallis', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97330', 1 FROM DUAL UNION ALL
SELECT 540, N'Hillsboro', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97123', 1 FROM DUAL UNION ALL
SELECT 541, N'Klamath Falls', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97601', 1 FROM DUAL UNION ALL
SELECT 542, N'Lake Oswego', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97034', 1 FROM DUAL UNION ALL
SELECT 543, N'Lebanon', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97355', 1 FROM DUAL UNION ALL
SELECT 544, N'Medford', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97504', 1 FROM DUAL UNION ALL
SELECT 545, N'Milwaukie', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97222', 1 FROM DUAL UNION ALL
SELECT 546, N'Oregon City', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97045', 1 FROM DUAL UNION ALL
SELECT 547, N'Portland', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97205', 1 FROM DUAL UNION ALL
SELECT 548, N'Salem', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97301', 1 FROM DUAL UNION ALL
SELECT 549, N'Springfield', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97477', 1 FROM DUAL UNION ALL
SELECT 550, N'Tigard', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97223', 1 FROM DUAL UNION ALL
SELECT 551, N'Troutdale', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97060', 1 FROM DUAL UNION ALL
SELECT 552, N'W. Linn', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97068', 1 FROM DUAL UNION ALL
SELECT 553, N'Woodburn', N'OR', N'Oregon', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'97071', 1 FROM DUAL UNION ALL
SELECT 554, N'Warwick', N'RI', N'Rhode Island', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'02889', 2 FROM DUAL UNION ALL
SELECT 555, N'West Kingston', N'RI', N'Rhode Island', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'02892', 2 FROM DUAL UNION ALL
SELECT 556, N'Woonsocket', N'RI', N'Rhode Island', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'02895', 2 FROM DUAL UNION ALL
SELECT 557, N'Bluffton', N'SC', N'South Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'29910', 5 FROM DUAL UNION ALL
SELECT 558, N'Gaffney', N'SC', N'South Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'29340', 5 FROM DUAL UNION ALL
SELECT 559, N'Myrtle Beach', N'SC', N'South Carolina', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'29577', 5 FROM DUAL UNION ALL
SELECT 560, N'Denby', N'SD', N'South Dakota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'57716', 3 FROM DUAL UNION ALL
SELECT 561, N'North Sioux City', N'SD', N'South Dakota', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'57049', 3 FROM DUAL UNION ALL
SELECT 562, N'Crossville', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'38555', 5 FROM DUAL UNION ALL
SELECT 563, N'Hixson', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'37343', 5 FROM DUAL UNION ALL
SELECT 564, N'Kingsport', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'37660', 5 FROM DUAL UNION ALL
SELECT 565, N'La Vergne', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'37086', 5 FROM DUAL UNION ALL
SELECT 566, N'Maryville', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'37801', 5 FROM DUAL UNION ALL
SELECT 567, N'Memphis', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'38103', 5 FROM DUAL UNION ALL
SELECT 568, N'Millington', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'38054', 5 FROM DUAL UNION ALL
SELECT 569, N'Nashville', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'37203', 5 FROM DUAL UNION ALL
SELECT 570, N'Pigeon Forge', N'TN', N'Tennessee', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'37863', 5 FROM DUAL UNION ALL
SELECT 571, N'Arlington', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'76010', 4 FROM DUAL UNION ALL
SELECT 572, N'Austin', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'78701', 4 FROM DUAL UNION ALL
SELECT 573, N'Baytown', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'77520', 4 FROM DUAL UNION ALL
SELECT 574, N'Carrollton', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'75006', 4 FROM DUAL UNION ALL
SELECT 575, N'Cedar Park', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'78613', 4 FROM DUAL UNION ALL
SELECT 576, N'College Station', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'77840', 4 FROM DUAL UNION ALL
SELECT 577, N'Corpus Christi', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'78404', 4 FROM DUAL UNION ALL
SELECT 578, N'Dallas', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'75201', 4 FROM DUAL UNION ALL
SELECT 579, N'Fort Worth', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'76102', 4 FROM DUAL UNION ALL
SELECT 580, N'Garland', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'75040', 4 FROM DUAL UNION ALL
SELECT 581, N'Hillsboro', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'76645', 4 FROM DUAL UNION ALL
SELECT 582, N'Houston', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'77003', 4 FROM DUAL UNION ALL
SELECT 583, N'Humble', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'77338', 4 FROM DUAL UNION ALL
SELECT 584, N'Irving', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'75061', 4 FROM DUAL UNION ALL
SELECT 585, N'Killeen', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'76541', 4 FROM DUAL UNION ALL
SELECT 586, N'La Marque', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'77568', 4 FROM DUAL UNION ALL
SELECT 587, N'Laredo', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'78040', 4 FROM DUAL UNION ALL
SELECT 588, N'Mesquite', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'75149', 4 FROM DUAL UNION ALL
SELECT 589, N'Plano', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'75074', 4 FROM DUAL UNION ALL
SELECT 590, N'Round Rock', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'78664', 4 FROM DUAL UNION ALL
SELECT 591, N'San Antonio', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'78204', 4 FROM DUAL UNION ALL
SELECT 592, N'Stafford', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'77477', 4 FROM DUAL UNION ALL
SELECT 593, N'Sugar Land', N'TX', N'Texas', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'77478', 4 FROM DUAL UNION ALL
SELECT 594, N'Bountiful', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84010', 1 FROM DUAL UNION ALL
SELECT 595, N'Cedar City', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84720', 1 FROM DUAL UNION ALL
SELECT 596, N'Ogden', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84401', 1 FROM DUAL UNION ALL
SELECT 597, N'Park City', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84098', 1 FROM DUAL UNION ALL
SELECT 598, N'Riverton', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84065', 1 FROM DUAL UNION ALL
SELECT 599, N'Salt Lake City', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84101', 1 FROM DUAL UNION ALL
SELECT 600, N'Sandy', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84070', 1 FROM DUAL UNION ALL
SELECT 601, N'Tooele', N'UT', N'Utah', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'84074', 1 FROM DUAL UNION ALL
SELECT 602, N'Chantilly', N'VA', N'Virginia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'20151', 5 FROM DUAL UNION ALL
SELECT 603, N'Falls Church', N'VA', N'Virginia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'22046', 5 FROM DUAL UNION ALL
SELECT 604, N'Leesburg', N'VA', N'Virginia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'20176', 5 FROM DUAL UNION ALL
SELECT 605, N'Newport News', N'VA', N'Virginia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'23607', 5 FROM DUAL UNION ALL
SELECT 606, N'Virginia Beach', N'VA', N'Virginia', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'23451', 5 FROM DUAL UNION ALL
SELECT 607, N'Ballard', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98107', 1 FROM DUAL UNION ALL
SELECT 608, N'Bellevue', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98004', 1 FROM DUAL UNION ALL
SELECT 609, N'Bellingham', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98225', 1 FROM DUAL UNION ALL
SELECT 610, N'Bothell', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98011', 1 FROM DUAL UNION ALL
SELECT 611, N'Bremerton', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98312', 1 FROM DUAL UNION ALL
SELECT 612, N'Burien', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98168', 1 FROM DUAL UNION ALL
SELECT 613, N'Chehalis', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98532', 1 FROM DUAL UNION ALL
SELECT 614, N'Edmonds', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98020', 1 FROM DUAL UNION ALL
SELECT 615, N'Ellensburg', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98926', 1 FROM DUAL UNION ALL
SELECT 616, N'Everett', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98201', 1 FROM DUAL UNION ALL
SELECT 617, N'Federal Way', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98003', 1 FROM DUAL UNION ALL
SELECT 618, N'Issaquah', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98027', 1 FROM DUAL UNION ALL
SELECT 619, N'Kelso', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98626', 1 FROM DUAL UNION ALL
SELECT 620, N'Kenmore', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98028', 1 FROM DUAL UNION ALL
SELECT 621, N'Kennewick', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'99337', 1 FROM DUAL UNION ALL
SELECT 622, N'Kent', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98031', 1 FROM DUAL UNION ALL
SELECT 623, N'Kirkland', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98033', 1 FROM DUAL UNION ALL
SELECT 624, N'Lacey', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98503', 1 FROM DUAL UNION ALL
SELECT 625, N'Longview', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98632', 1 FROM DUAL UNION ALL
SELECT 626, N'Lynnwood', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98036', 1 FROM DUAL UNION ALL
SELECT 627, N'Marysville', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98270', 1 FROM DUAL UNION ALL
SELECT 628, N'Monroe', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98272', 1 FROM DUAL UNION ALL
SELECT 629, N'Newport Hills', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98006', 1 FROM DUAL UNION ALL
SELECT 630, N'North Bend', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98045', 1 FROM DUAL UNION ALL
SELECT 631, N'Olympia', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98501', 1 FROM DUAL UNION ALL
SELECT 632, N'Port Orchard', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98366', 1 FROM DUAL UNION ALL
SELECT 633, N'Puyallup', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98371', 1 FROM DUAL UNION ALL
SELECT 634, N'Redmond', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98052', 1 FROM DUAL UNION ALL
SELECT 635, N'Renton', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98055', 1 FROM DUAL UNION ALL
SELECT 636, N'Sammamish', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98074', 1 FROM DUAL UNION ALL
SELECT 637, N'Seattle', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98104', 1 FROM DUAL UNION ALL
SELECT 638, N'Sedro Woolley', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98284', 1 FROM DUAL UNION ALL
SELECT 639, N'Sequim', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98382', 1 FROM DUAL UNION ALL
SELECT 640, N'Shelton', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98584', 1 FROM DUAL UNION ALL
SELECT 641, N'Spokane', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'99202', 1 FROM DUAL UNION ALL
SELECT 642, N'Tacoma', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98403', 1 FROM DUAL UNION ALL
SELECT 643, N'Union Gap', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98903', 1 FROM DUAL UNION ALL
SELECT 644, N'Walla Walla', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'99362', 1 FROM DUAL UNION ALL
SELECT 645, N'Washougal', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98671', 1 FROM DUAL UNION ALL
SELECT 646, N'Wenatchee', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98801', 1 FROM DUAL UNION ALL
SELECT 647, N'Woodinville', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98072', 1 FROM DUAL UNION ALL
SELECT 648, N'Yakima', N'WA', N'Washington', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'98901', 1 FROM DUAL UNION ALL
SELECT 649, N'Johnson Creek', N'WI', N'Wisconsin', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'53038', 3 FROM DUAL UNION ALL
SELECT 650, N'Milwaukee', N'WI', N'Wisconsin', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'53202', 3 FROM DUAL UNION ALL
SELECT 651, N'Mosinee', N'WI', N'Wisconsin', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'54455', 3 FROM DUAL UNION ALL
SELECT 652, N'Racine', N'WI', N'Wisconsin', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'53182', 3 FROM DUAL UNION ALL
SELECT 653, N'Casper', N'WY', N'Wyoming', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'82601', 1 FROM DUAL UNION ALL
SELECT 654, N'Cheyenne', N'WY', N'Wyoming', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'82001', 1 FROM DUAL UNION ALL
SELECT 655, N'Rock Springs', N'WY', N'Wyoming', N'US', N'United States', N'Estados Unidos', N'États-Unis', N'82901', 1 FROM DUAL;

COMMIT;