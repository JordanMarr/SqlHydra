module SqlHydra.Query.MySqlExtensions

open System

/// Common MySQL functions for use in select expressions.
/// Use `open type SqlFn` to access functions without qualification.
[<SqlHydraFunction>]
type SqlFn =
    // String functions
    static member CHAR_LENGTH(s: string) : int = sqlFn
    static member CHARACTER_LENGTH(s: string) : int = sqlFn
    static member LENGTH(s: string) : int = sqlFn
    static member UPPER(s: string) : string = sqlFn
    static member LOWER(s: string) : string = sqlFn
    static member LTRIM(s: string) : string = sqlFn
    static member RTRIM(s: string) : string = sqlFn
    static member TRIM(s: string) : string = sqlFn
    static member SUBSTRING(s: string, start: int) : string = sqlFn
    static member SUBSTRING(s: string, start: int, length: int) : string = sqlFn
    static member SUBSTR(s: string, start: int, length: int) : string = sqlFn
    static member MID(s: string, start: int, length: int) : string = sqlFn
    static member REPLACE(s: string, from: string, ``to``: string) : string = sqlFn
    static member INSTR(s: string, substring: string) : int = sqlFn
    static member LOCATE(substring: string, s: string) : int = sqlFn
    static member LOCATE(substring: string, s: string, start: int) : int = sqlFn
    static member POSITION(substring: string, s: string) : int = sqlFn
    static member CONCAT(s1: string, s2: string) : string = sqlFn
    static member CONCAT(s1: string, s2: string, s3: string) : string = sqlFn
    static member CONCAT(s1: string, s2: string, s3: string, s4: string) : string = sqlFn
    static member CONCAT_WS(separator: string, s1: string, s2: string) : string = sqlFn
    static member CONCAT_WS(separator: string, s1: string, s2: string, s3: string) : string = sqlFn
    static member LEFT(s: string, length: int) : string = sqlFn
    static member RIGHT(s: string, length: int) : string = sqlFn
    static member REVERSE(s: string) : string = sqlFn
    static member REPEAT(s: string, count: int) : string = sqlFn
    static member LPAD(s: string, length: int, pad: string) : string = sqlFn
    static member RPAD(s: string, length: int, pad: string) : string = sqlFn
    static member SPACE(count: int) : string = sqlFn
    static member ASCII(s: string) : int = sqlFn
    static member CHAR(code: int) : string = sqlFn

    // Null handling - with overloads for Option and Nullable
    static member IFNULL(expr: Option<'T>, replacement: 'T) : 'T = sqlFn
    static member IFNULL(expr: Nullable<'T>, replacement: 'T) : 'T when 'T : struct = sqlFn
    static member IFNULL(expr: 'T, replacement: 'T) : 'T = sqlFn
    static member COALESCE(a: Option<'T>, b: 'T) : 'T = sqlFn
    static member COALESCE(a: Nullable<'T>, b: 'T) : 'T when 'T : struct = sqlFn
    static member COALESCE(a: 'T, b: 'T) : 'T = sqlFn
    static member COALESCE(a: 'T, b: 'T, c: 'T) : 'T = sqlFn
    static member NULLIF(a: 'T, b: 'T) : Option<'T> = sqlFn
    static member IF(condition: bool, trueValue: 'T, falseValue: 'T) : 'T = sqlFn

    // Numeric functions
    static member ABS(n: 'T) : 'T when 'T : struct = sqlFn
    static member ROUND(n: 'T) : 'T when 'T : struct = sqlFn
    static member ROUND(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn
    static member CEIL(n: 'T) : 'T when 'T : struct = sqlFn
    static member CEILING(n: 'T) : 'T when 'T : struct = sqlFn
    static member FLOOR(n: 'T) : 'T when 'T : struct = sqlFn
    static member SIGN(n: 'T) : int when 'T : struct = sqlFn
    static member POWER(n: 'T, exponent: 'T) : 'T when 'T : struct = sqlFn
    static member POW(n: 'T, exponent: 'T) : 'T when 'T : struct = sqlFn
    static member SQRT(n: 'T) : float when 'T : struct = sqlFn
    static member MOD(n: 'T, divisor: 'T) : 'T when 'T : struct = sqlFn
    static member TRUNCATE(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn
    static member RAND() : float = sqlFn

    // Date/time functions
    static member NOW() : DateTime = sqlFn
    static member CURDATE() : DateTime = sqlFn
    static member CURRENT_DATE() : DateTime = sqlFn
    static member CURTIME() : TimeSpan = sqlFn
    static member CURRENT_TIME() : TimeSpan = sqlFn
    static member CURRENT_TIMESTAMP() : DateTime = sqlFn
    static member SYSDATE() : DateTime = sqlFn
    static member EXTRACT(field: string, source: DateTime) : int = sqlFn
    static member YEAR(date: DateTime) : int = sqlFn
    static member MONTH(date: DateTime) : int = sqlFn
    static member DAY(date: DateTime) : int = sqlFn
    static member DAYOFWEEK(date: DateTime) : int = sqlFn
    static member DAYOFYEAR(date: DateTime) : int = sqlFn
    static member HOUR(time: DateTime) : int = sqlFn
    static member MINUTE(time: DateTime) : int = sqlFn
    static member SECOND(time: DateTime) : int = sqlFn
    static member DATE_ADD(date: DateTime, interval: string) : DateTime = sqlFn
    static member DATE_SUB(date: DateTime, interval: string) : DateTime = sqlFn
    static member DATEDIFF(date1: DateTime, date2: DateTime) : int = sqlFn
    static member TIMESTAMPDIFF(unit: string, date1: DateTime, date2: DateTime) : int = sqlFn
    static member DATE_FORMAT(date: DateTime, format: string) : string = sqlFn
    static member LAST_DAY(date: DateTime) : DateTime = sqlFn

    // Conversion functions
    static member CAST(value: 'T, asType: string) : 'U = sqlFn
    static member CONVERT(value: 'T, asType: string) : 'U = sqlFn
