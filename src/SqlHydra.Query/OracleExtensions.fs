module SqlHydra.Query.OracleExtensions

open System

/// Common Oracle functions for use in select expressions.
/// Use `open type SqlFn` to access functions without qualification.
[<SqlHydraFunction>]
type SqlFn =
    // String functions
    static member LENGTH(s: string) : int = sqlFn
    static member UPPER(s: string) : string = sqlFn
    static member LOWER(s: string) : string = sqlFn
    static member LTRIM(s: string) : string = sqlFn
    static member RTRIM(s: string) : string = sqlFn
    static member TRIM(s: string) : string = sqlFn
    static member SUBSTR(s: string, start: int) : string = sqlFn
    static member SUBSTR(s: string, start: int, length: int) : string = sqlFn
    static member REPLACE(s: string, search: string, replacement: string) : string = sqlFn
    static member INSTR(s: string, substring: string) : int = sqlFn
    static member INSTR(s: string, substring: string, start: int) : int = sqlFn
    static member INSTR(s: string, substring: string, start: int, occurrence: int) : int = sqlFn
    static member CONCAT(s1: string, s2: string) : string = sqlFn
    static member LPAD(s: string, length: int) : string = sqlFn
    static member LPAD(s: string, length: int, pad: string) : string = sqlFn
    static member RPAD(s: string, length: int) : string = sqlFn
    static member RPAD(s: string, length: int, pad: string) : string = sqlFn
    static member REVERSE(s: string) : string = sqlFn
    static member INITCAP(s: string) : string = sqlFn
    static member TRANSLATE(s: string, from: string, ``to``: string) : string = sqlFn
    static member ASCII(s: string) : int = sqlFn
    static member CHR(code: int) : string = sqlFn

    // Null handling - with overloads for Option and Nullable
    static member NVL(expr: Option<'T>, replacement: 'T) : 'T = sqlFn
    static member NVL(expr: Nullable<'T>, replacement: 'T) : 'T when 'T : struct = sqlFn
    static member NVL(expr: 'T, replacement: 'T) : 'T = sqlFn
    static member NVL2(expr: 'T, notNullValue: 'U, nullValue: 'U) : 'U = sqlFn
    static member COALESCE(a: Option<'T>, b: 'T) : 'T = sqlFn
    static member COALESCE(a: Nullable<'T>, b: 'T) : 'T when 'T : struct = sqlFn
    static member COALESCE(a: 'T, b: 'T) : 'T = sqlFn
    static member COALESCE(a: 'T, b: 'T, c: 'T) : 'T = sqlFn
    static member NULLIF(a: 'T, b: 'T) : Option<'T> = sqlFn
    static member DECODE(expr: 'T, search: 'T, result: 'U, defaultValue: 'U) : 'U = sqlFn

    // Numeric functions
    static member ABS(n: 'T) : 'T when 'T : struct = sqlFn
    static member ROUND(n: 'T) : 'T when 'T : struct = sqlFn
    static member ROUND(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn
    static member CEIL(n: 'T) : 'T when 'T : struct = sqlFn
    static member FLOOR(n: 'T) : 'T when 'T : struct = sqlFn
    static member SIGN(n: 'T) : int when 'T : struct = sqlFn
    static member POWER(n: 'T, exponent: 'T) : 'T when 'T : struct = sqlFn
    static member SQRT(n: 'T) : float when 'T : struct = sqlFn
    static member MOD(n: 'T, divisor: 'T) : 'T when 'T : struct = sqlFn
    static member TRUNC(n: 'T) : 'T when 'T : struct = sqlFn
    static member TRUNC(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn

    // Date/time functions
    static member SYSDATE() : DateTime = sqlFn
    static member SYSTIMESTAMP() : DateTime = sqlFn
    static member CURRENT_DATE() : DateTime = sqlFn
    static member CURRENT_TIMESTAMP() : DateTime = sqlFn
    static member EXTRACT(field: string, source: DateTime) : int = sqlFn
    static member ADD_MONTHS(date: DateTime, months: int) : DateTime = sqlFn
    static member MONTHS_BETWEEN(date1: DateTime, date2: DateTime) : float = sqlFn
    static member NEXT_DAY(date: DateTime, dayOfWeek: string) : DateTime = sqlFn
    static member LAST_DAY(date: DateTime) : DateTime = sqlFn
    static member TRUNC(date: DateTime) : DateTime = sqlFn
    static member TRUNC(date: DateTime, format: string) : DateTime = sqlFn

    // Conversion functions
    static member TO_CHAR(value: 'T) : string = sqlFn
    static member TO_CHAR(value: 'T, format: string) : string = sqlFn
    static member TO_DATE(s: string, format: string) : DateTime = sqlFn
    static member TO_NUMBER(s: string) : decimal = sqlFn
    static member TO_NUMBER(s: string, format: string) : decimal = sqlFn
