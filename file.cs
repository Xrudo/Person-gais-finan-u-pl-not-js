using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonigaisFinansuPlanotajs
{
    // Enums and Exceptions
    public enum Category { Food, Transport, Fun, School, Other }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    // Models
    public class Income
    {
        public DateTime Date { get; set; }
        public string Source { get; set; }
        public decimal Amount { get; set; }

        public Income() { }

        public Income(string dateStr, string source, string amountStr)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ValidationException("Income: Source cannot be empty.");
            if (!DateTime.TryParse(dateStr, out var d)) throw new ValidationException("Income: Invalid date.");
            if (!Tools.SafeParseDecimal(amountStr, out var a) || a <= 0) throw new ValidationException("Income: Amount must be a number greater than 0.");
            Date = d.Date;
            Source = source.Trim();
            Amount = a;
        }
    }

    public class Expense
    {
        public DateTime Date { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Category Category { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }

        public Expense() { }

        public Expense(string dateStr, string categoryStr, string amountStr, string note)
        {
            if (!DateTime.TryParse(dateStr, out var d)) throw new ValidationException("Expense: Invalid date.");
            if (string.IsNullOrWhiteSpace(note)) note = string.Empty; // note may be empty
            if (!Enum.TryParse<Category>(categoryStr, true, out var cat)) throw new ValidationException("Expense: Invalid category.");
            if (!Tools.SafeParseDecimal(amountStr, out var a) || a <= 0) throw new ValidationException("Expense: Amount must be a number greater than 0.");
            Date = d.Date;
            Category = cat;
            Amount = a;
            Note = note.Trim();
        }
    }

    public class Subscription
    {
        public string Name { get; set; }
        public decimal MonthlyPrice { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsActive { get; set; }

        public Subscription() { }

        public Subscription(string name, string monthlyPriceStr, string startDateStr, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Subscription: Name cannot be empty.");
            if (!Tools.SafeParseDecimal(monthlyPriceStr, out var p) || p <= 0) throw new ValidationException("Subscription: Monthly price must be a number greater than 0.");
            if (!DateTime.TryParse(startDateStr, out var d)) throw new ValidationException("Subscription: Invalid start date.");
            Name = name.Trim();
            MonthlyPrice = p;
            StartDate = d.Date;
            IsActive = isActive;
        }
    }

    // Wrapper for JSON import/export
    public class DataBundle
    {
        public List<Income> Incomes { get; set; } = new List<Income>();
        public List<Expense> Expenses { get; set; } = new List<Expense>();
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }

    // Helper tools
    public static class Tools
    {
        public static bool SafeParseDecimal(string s, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return decimal.TryParse(s.Trim(), out value);
        }

        public static decimal SafeDivide(decimal numerator, decimal denominator)
        {
            if (denominator == 0) return 0m;
            return numerator / denominator;
        }

        public static string Percent(decimal part, decimal total)
        {
            if (total == 0) return "0%";
            var p = SafeDivide(part, total) * 100m;
            return Math.Round(p, 1) + "%";
        }

        public static string ReadNonEmptyString(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                Console.WriteLine("Input cannot be empty. Try again.");
            }
        }

        public static void PrintCurrency(decimal amount)
        {
            Console.Write(amount.ToString("C2") + ""); // will include currency symbol based on culture; we'll append € if missing
        }

        public static string FormatCurrency(decimal amount)
        {
            // Force Euro symbol and two decimals
            return string.Format("€{0:N2}", amount);
        }

        public static void Pause()
        {
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
    }

    class Program
    {
        static List<Income> incomes = new List<Income>();
        static List<Expense> expenses = new List<Expense>();
        static List<Subscription> subscriptions = new List<Subscription>();

        static JsonSerializerOptions jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            ShowHelpShort();
            while (true)
            {
                try
                {
                    Console.Clear();
                    Console.WriteLine("=== Personīgais finanšu plānotājs ===");
                    Console.WriteLine("1) Ienākumi");
                    Console.WriteLine("2) Izdevumi");
                    Console.WriteLine("3) Abonementi");
                    Console.WriteLine("4) Saraksti");
                    Console.WriteLine("5) Filtri");
                    Console.WriteLine("6) Mēneša pārskats");
                    Console.WriteLine("7) Import/Export JSON");
                    Console.WriteLine("H) Palīdzība");
                    Console.WriteLine("0) Iziet");
                    Console.Write("Izvēle: ");
                    var choice = Console.ReadLine()?.Trim();
                    switch (choice)
                    {
                        case "1": IncomesMenu(); break;
                        case "2": ExpensesMenu(); break;
                        case "3": SubscriptionsMenu(); break;
                        case "4": ListsMenu(); break;
                        case "5": FiltersMenu(); break;
                        case "6": MonthReportMenu(); break;
                        case "7": JsonMenu(); break;
                        case "H":
                        case "h": ShowHelp(); break;
                        case "0": return;
                        default:
                            Console.WriteLine("Nezināma izvēle.");
                            Tools.Pause();
                            break;
                    }
                }
                catch (ValidationException vex)
                {
                    Console.WriteLine("Validācijas kļūda: " + vex.Message);
                    Tools.Pause();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nezināma kļūda: " + ex.Message);
                    Tools.Pause();
                }
            }
        }

        #region Menus
        static void IncomesMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- Ienākumi ---");
                Console.WriteLine("1) Pievienot");
                Console.WriteLine("2) Attēlot");
                Console.WriteLine("3) Dzēst");
                Console.WriteLine("0) Atpakaļ");
                Console.Write("Izvēle: ");
                var c = Console.ReadLine()?.Trim();
                if (c == "1") AddIncome();
                else if (c == "2") ShowIncomes();
                else if (c == "3") DeleteIncome();
                else if (c == "0") return;
                else { Console.WriteLine("Nezināma izvēle."); Tools.Pause(); }
            }
        }

        static void ExpensesMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- Izdevumi ---");
                Console.WriteLine("1) Pievienot");
                Console.WriteLine("2) Attēlot");
                Console.WriteLine("3) Dzēst");
                Console.WriteLine("4) Filtrēt (pēc datuma/kategorijas)");
                Console.WriteLine("0) Atpakaļ");
                Console.Write("Izvēle: ");
                var c = Console.ReadLine()?.Trim();
                if (c == "1") AddExpense();
                else if (c == "2") ShowExpenses();
                else if (c == "3") DeleteExpense();
                else if (c == "4") ExpensesFilterMenu();
                else if (c == "0") return;
                else { Console.WriteLine("Nezināma izvēle."); Tools.Pause(); }
            }
        }

        static void SubscriptionsMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- Abonementi ---");
                Console.WriteLine("1) Pievienot");
                Console.WriteLine("2) Attēlot");
                Console.WriteLine("3) Aktivizēt/Deaktivizēt");
                Console.WriteLine("4) Dzēst");
                Console.WriteLine("0) Atpakaļ");
                Console.Write("Izvēle: ");
                var c = Console.ReadLine()?.Trim();
                if (c == "1") AddSubscription();
                else if (c == "2") ShowSubscriptions();
                else if (c == "3") ToggleSubscription();
                else if (c == "4") DeleteSubscription();
                else if (c == "0") return;
                else { Console.WriteLine("Nezināma izvēle."); Tools.Pause(); }
            }
        }

        static void ListsMenu()
        {
            Console.Clear();
            Console.WriteLine("--- Saraksti (visi ieraksti sakārtoti pēc datuma, dilstoši) ---");
            var all = new List<(DateTime Date, string Type, string Text, decimal Amount)>();
            all.AddRange(incomes.Select(i => (i.Date, "Income", i.Source, i.Amount)));
            all.AddRange(expenses.Select(e => (e.Date, "Expense", e.Category + " " + e.Note, e.Amount)));
            all.AddRange(subscriptions.Select(s => (s.StartDate, "Subscription", s.Name + (s.IsActive ? " (active)" : " (inactive)"), s.MonthlyPrice)));
            foreach (var item in all.OrderByDescending(a => a.Date))
            {
                Console.WriteLine($"{item.Date:yyyy-MM-dd} | {item.Type,-12} | {item.Text,-25} | {Tools.FormatCurrency(item.Amount),10}");
            }
            Tools.Pause();
        }

        static void FiltersMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- Filtri ---");
                Console.WriteLine("1) Pēc datuma diapazona (visi ieraksti)");
                Console.WriteLine("2) Izdevumi pēc kategorijas");
                Console.WriteLine("0) Atpakaļ");
                Console.Write("Izvēle: ");
                var c = Console.ReadLine()?.Trim();
                if (c == "1") FilterByDateRange();
                else if (c == "2") FilterExpensesByCategory();
                else if (c == "0") return;
                else { Console.WriteLine("Nezināma izvēle."); Tools.Pause(); }
            }
        }

        static void MonthReportMenu()
        {
            Console.Clear();
            Console.WriteLine("--- Mēneša pārskats (ievadiet formātu: YYYY-MM) ---");
            Console.Write("Mēnesis: ");
            var m = Console.ReadLine()?.Trim();
            if (!TryParseYearMonth(m, out var year, out var month))
            {
                Console.WriteLine("Nezināms formāts. Piemērs: 2025-09");
                Tools.Pause();
                return;
            }
            GenerateMonthReport(year, month);
            Tools.Pause();
        }

        static void JsonMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- Import/Export JSON ---");
                Console.WriteLine("1) Eksportēt JSON (izvade konsolē)");
                Console.WriteLine("2) Importēt JSON (ielīmējiet JSON un pēc tam tukšu rindu)");
                Console.WriteLine("0) Atpakaļ");
                Console.Write("Izvēle: ");
                var c = Console.ReadLine()?.Trim();
                if (c == "1") ExportJson();
                else if (c == "2") ImportJson();
                else if (c == "0") return;
                else { Console.WriteLine("Nezināma izvēle."); Tools.Pause(); }
            }
        }
        #endregion

        #region Income CRUD
        static void AddIncome()
        {
            Console.Clear();
            Console.WriteLine("Pievienot ienākumu:");
            var date = Tools.ReadNonEmptyString("Datums (yyyy-MM-dd): ");
            var source = Tools.ReadNonEmptyString("Avots: ");
            Console.Write("Summa: ");
            var sum = Console.ReadLine();
            var inc = new Income(date, source, sum);
            incomes.Add(inc);
            Console.WriteLine("Ienākums pievienots.");
            Tools.Pause();
        }

        static void ShowIncomes()
        {
            Console.Clear();
            Console.WriteLine("--- Ienākumi ---");
            var sorted = incomes.OrderByDescending(i => i.Date).ToList();
            if (!sorted.Any()) Console.WriteLine("Nav ierakstu.");
            else
            {
                Console.WriteLine("# | Date       | Source                   | Amount");
                for (int i = 0; i < sorted.Count; i++)
                {
                    var it = sorted[i];
                    Console.WriteLine($"{i+1,2} | {it.Date:yyyy-MM-dd} | {it.Source,-24} | {Tools.FormatCurrency(it.Amount),10}");
                }
            }
            Tools.Pause();
        }

        static void DeleteIncome()
        {
            ShowIncomes();
            Console.Write("Ievadiet dzēšamā ieraksta numuru (0 - atpakaļ): ");
            if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 0) { Console.WriteLine("Kļūda."); Tools.Pause(); return; }
            if (idx == 0) return;
            var sorted = incomes.OrderByDescending(i => i.Date).ToList();
            if (idx > sorted.Count) { Console.WriteLine("Nepareizs indekss."); Tools.Pause(); return; }
            var item = sorted[idx - 1];
            incomes.Remove(item);
            Console.WriteLine("Ieraksts dzēsts.");
            Tools.Pause();
        }
        #endregion

        #region Expense CRUD
        static void AddExpense()
        {
            Console.Clear();
            Console.WriteLine("Pievienot izdevumu:");
            var date = Tools.ReadNonEmptyString("Datums (yyyy-MM-dd): ");
            Console.WriteLine("Kategorijas: " + string.Join(", ", Enum.GetNames(typeof(Category))));
            var cat = Tools.ReadNonEmptyString("Kategorija: ");
            Console.Write("Summa: ");
            var sum = Console.ReadLine();
            Console.Write("Piezīme (var būt tukša): ");
            var note = Console.ReadLine() ?? string.Empty;
            var exp = new Expense(date, cat, sum, note);
            expenses.Add(exp);
            Console.WriteLine("Izdevums pievienots.");
            Tools.Pause();
        }

        static void ShowExpenses()
        {
            Console.Clear();
            Console.WriteLine("--- Izdevumi ---");
            var sorted = expenses.OrderByDescending(e => e.Date).ToList();
            if (!sorted.Any()) Console.WriteLine("Nav ierakstu.");
            else
            {
                Console.WriteLine("# | Date       | Category   | Note                      | Amount");
                for (int i = 0; i < sorted.Count; i++)
                {
                    var it = sorted[i];
                    Console.WriteLine($"{i+1,2} | {it.Date:yyyy-MM-dd} | {it.Category,-10} | {it.Note,-24} | {Tools.FormatCurrency(it.Amount),10}");
                }
            }
            Tools.Pause();
        }

        static void DeleteExpense()
        {
            ShowExpenses();
            Console.Write("Ievadiet dzēšamā ieraksta numuru (0 - atpakaļ): ");
            if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 0) { Console.WriteLine("Kļūda."); Tools.Pause(); return; }
            if (idx == 0) return;
            var sorted = expenses.OrderByDescending(i => i.Date).ToList();
            if (idx > sorted.Count) { Console.WriteLine("Nepareizs indekss."); Tools.Pause(); return; }
            var item = sorted[idx - 1];
            expenses.Remove(item);
            Console.WriteLine("Ieraksts dzēsts.");
            Tools.Pause();
        }

        static void ExpensesFilterMenu()
        {
            Console.Clear();
            Console.WriteLine("Filtrēt izdevumus pēc datuma vai kategorijas:");
            Console.WriteLine("1) Pēc datuma diapazona");
            Console.WriteLine("2) Pēc kategorijas");
            Console.Write("Izvēle: ");
            var c = Console.ReadLine()?.Trim();
            if (c == "1")
            {
                Console.Write("No (yyyy-MM-dd): "); var from = Console.ReadLine();
                Console.Write("Līdz (yyyy-MM-dd): "); var to = Console.ReadLine();
                if (!DateTime.TryParse(from, out var f) || !DateTime.TryParse(to, out var t)) { Console.WriteLine("Nepareizi datumi."); Tools.Pause(); return; }
                var res = expenses.Where(e => e.Date.Date >= f.Date && e.Date.Date <= t.Date).OrderByDescending(e => e.Date).ToList();
                PrintExpenseListWithSum(res);
            }
            else if (c == "2")
            {
                Console.WriteLine("Kategorijas: " + string.Join(", ", Enum.GetNames(typeof(Category))));
                var cat = Console.ReadLine();
                if (!Enum.TryParse<Category>(cat, true, out var category)) { Console.WriteLine("Nederīga kategorija."); Tools.Pause(); return; }
                var res = expenses.Where(e => e.Category == category).OrderByDescending(e => e.Date).ToList();
                PrintExpenseListWithSum(res);
            }
            else { Console.WriteLine("Nezināma izvēle."); Tools.Pause(); }
        }

        static void PrintExpenseListWithSum(List<Expense> list)
        {
            if (!list.Any()) Console.WriteLine("Nav ierakstu.");
            else
            {
                Console.WriteLine("# | Date       | Category   | Note                      | Amount");
                for (int i = 0; i < list.Count; i++)
                {
                    var it = list[i];
                    Console.WriteLine($"{i+1,2} | {it.Date:yyyy-MM-dd} | {it.Category,-10} | {it.Note,-24} | {Tools.FormatCurrency(it.Amount),10}");
                }
                var sum = list.Sum(x => x.Amount);
                Console.WriteLine($"Kopsumma: {Tools.FormatCurrency(sum)}");
            }
            Tools.Pause();
        }
        #endregion

        #region Subscriptions
        static void AddSubscription()
        {
            Console.Clear();
            Console.WriteLine("Pievienot abonementu:");
            var name = Tools.ReadNonEmptyString("Nosaukums: ");
            Console.Write("Mēneša cena: "); var price = Console.ReadLine();
            var startDate = Tools.ReadNonEmptyString("Sākuma datums (yyyy-MM-dd): ");
            Console.Write("Aktīvs? (j/n): "); var a = Console.ReadLine();
            var active = (!string.IsNullOrWhiteSpace(a) && (a.Trim().ToLower() == "j" || a.Trim().ToLower() == "y"));
            var s = new Subscription(name, price, startDate, active);
            subscriptions.Add(s);
            Console.WriteLine("Abonementa ieraksts pievienots.");
            Tools.Pause();
        }

        static void ShowSubscriptions()
        {
            Console.Clear();
            Console.WriteLine("--- Abonementi ---");
            var sorted = subscriptions.OrderByDescending(s => s.StartDate).ToList();
            if (!sorted.Any()) Console.WriteLine("Nav ierakstu.");
            else
            {
                Console.WriteLine("# | StartDate  | Name                       | Price     | Active");
                for (int i = 0; i < sorted.Count; i++)
                {
                    var it = sorted[i];
                    Console.WriteLine($"{i+1,2} | {it.StartDate:yyyy-MM-dd} | {it.Name,-25} | {Tools.FormatCurrency(it.MonthlyPrice),10} | {(it.IsActive?"Yes":"No")}");
                }
            }
            Tools.Pause();
        }

        static void ToggleSubscription()
        {
            ShowSubscriptions();
            Console.Write("Ievadiet ieraksta numuru, ko toggle (0 - atpakaļ): ");
            if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 0) { Console.WriteLine("Kļūda."); Tools.Pause(); return; }
            if (idx == 0) return;
            var sorted = subscriptions.OrderByDescending(s => s.StartDate).ToList();
            if (idx > sorted.Count) { Console.WriteLine("Nepareizs indekss."); Tools.Pause(); return; }
            var item = sorted[idx - 1];
            item.IsActive = !item.IsActive;
            Console.WriteLine($"Abonements '{item.Name}' tagad {(item.IsActive?"aktīvs":"neaktīvs")}.");
            Tools.Pause();
        }

        static void DeleteSubscription()
        {
            ShowSubscriptions();
            Console.Write("Ievadiet dzēšamā ieraksta numuru (0 - atpakaļ): ");
            if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 0) { Console.WriteLine("Kļūda."); Tools.Pause(); return; }
            if (idx == 0) return;
            var sorted = subscriptions.OrderByDescending(s => s.StartDate).ToList();
            if (idx > sorted.Count) { Console.WriteLine("Nepareizs indekss."); Tools.Pause(); return; }
            var item = sorted[idx - 1];
            subscriptions.Remove(item);
            Console.WriteLine("Ieraksts dzēsts.");
            Tools.Pause();
        }
        #endregion

        #region Filters & Reports
        static void FilterByDateRange()
        {
            Console.Write("No (yyyy-MM-dd): "); var from = Console.ReadLine();
            Console.Write("Līdz (yyyy-MM-dd): "); var to = Console.ReadLine();
            if (!DateTime.TryParse(from, out var f) || !DateTime.TryParse(to, out var t)) { Console.WriteLine("Nepareizi datumi."); Tools.Pause(); return; }
            var inc = incomes.Where(i => i.Date.Date >= f.Date && i.Date.Date <= t.Date).OrderByDescending(i => i.Date).ToList();
            var exp = expenses.Where(e => e.Date.Date >= f.Date && e.Date.Date <= t.Date).OrderByDescending(e => e.Date).ToList();
            Console.WriteLine("-- Ienākumi --");
            foreach (var i in inc) Console.WriteLine($"{i.Date:yyyy-MM-dd} | {i.Source,-20} | {Tools.FormatCurrency(i.Amount),10}");
            Console.WriteLine("-- Izdevumi --");
            foreach (var e in exp) Console.WriteLine($"{e.Date:yyyy-MM-dd} | {e.Category,-10} | {e.Note,-20} | {Tools.FormatCurrency(e.Amount),10}");
            Console.WriteLine($"Ienākumu kopsumma: {Tools.FormatCurrency(inc.Sum(x=>x.Amount))}");
            Console.WriteLine($"Izdevumu kopsumma: {Tools.FormatCurrency(exp.Sum(x=>x.Amount))}");
            Tools.Pause();
        }

        static void FilterExpensesByCategory()
        {
            Console.WriteLine("Kategorijas: " + string.Join(", ", Enum.GetNames(typeof(Category))));
            var cat = Tools.ReadNonEmptyString("Kategorija: ");
            if (!Enum.TryParse<Category>(cat, true, out var category)) { Console.WriteLine("Nederīga kategorija."); Tools.Pause(); return; }
            var res = expenses.Where(e => e.Category == category).OrderByDescending(e => e.Date).ToList();
            PrintExpenseListWithSum(res);
        }

        static void GenerateMonthReport(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var inc = incomes.Where(i => i.Date.Date >= monthStart && i.Date.Date <= monthEnd).ToList();
            var exp = expenses.Where(e => e.Date.Date >= monthStart && e.Date.Date <= monthEnd).ToList();
            var activeSubs = subscriptions.Where(s => s.IsActive && s.StartDate <= monthEnd).ToList();

            var incSum = inc.Sum(i => i.Amount);
            var expSum = exp.Sum(e => e.Amount);
            var subsSum = activeSubs.Sum(s => s.MonthlyPrice);
            var net = incSum - expSum - subsSum;

            Console.WriteLine($"Pārskats par {year}-{month:D2}");
            Console.WriteLine($"Ienākumi: {Tools.FormatCurrency(incSum)}");
            Console.WriteLine($"Izdevumi: {Tools.FormatCurrency(expSum)}");
            Console.WriteLine($"Aktīvie abonementi: {Tools.FormatCurrency(subsSum)} (Skaits: {activeSubs.Count})");
            Console.WriteLine($"Neto: {Tools.FormatCurrency(net)}");

            // Category percentages
            Console.WriteLine("--- Kategoriju sadalījums ---");
            var byCat = exp.GroupBy(e => e.Category).Select(g => new { Cat = g.Key, Sum = g.Sum(x => x.Amount) }).ToList();
            foreach (var b in byCat)
            {
                Console.WriteLine($"{b.Cat,-10} {Tools.FormatCurrency(b.Sum),10} ({Tools.Percent(b.Sum, expSum)})");
            }

            // Largest expense
            if (exp.Any())
            {
                var largest = exp.OrderByDescending(e => e.Amount).First();
                Console.WriteLine($"Lielākais izdevums: {largest.Date:yyyy-MM-dd} | {largest.Category} | {largest.Note} | {Tools.FormatCurrency(largest.Amount)}");
            }
            else Console.WriteLine("Nav izdevumu.");

            // Average daily expense in month (consider days in month)
            var days = (monthEnd - monthStart).Days + 1;
            var avgDaily = Tools.SafeDivide(expSum, days);
            Console.WriteLine($"Vidējie dienas tēriņi: {Tools.FormatCurrency(avgDaily)} (dienu skaits: {days})");
        }

        static bool TryParseYearMonth(string s, out int year, out int month)
        {
            year = 0; month = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split('-');
            if (parts.Length != 2) return false;
            return int.TryParse(parts[0], out year) && int.TryParse(parts[1], out month) && month >= 1 && month <= 12;
        }
        #endregion

        #region JSON Import/Export
        static void ExportJson()
        {
            var bundle = new DataBundle
            {
                Incomes = incomes,
                Expenses = expenses,
                Subscriptions = subscriptions
            };
            var json = JsonSerializer.Serialize(bundle, jsonOpts);
            Console.WriteLine("--- JSON Eksports ---");
            Console.WriteLine(json);
            Tools.Pause();
        }

        static void ImportJson()
        {
            Console.WriteLine("Ielīmējiet JSON saturu un nospiediet Enter, pēc tam atstājiet tukšu rindu:");
            var lines = new List<string>();
            while (true)
            {
                var line = Console.ReadLine();
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) break;
                lines.Add(line);
            }
            var input = string.Join(Environment.NewLine, lines);
            if (string.IsNullOrWhiteSpace(input)) { Console.WriteLine("Nav ievadīts JSON."); Tools.Pause(); return; }

            try
            {
                var parsed = JsonSerializer.Deserialize<DataBundle>(input, jsonOpts);
                if (parsed == null) throw new ValidationException("JSON nevar tikt parsēts.");
                // Validate all entries before replacing
                ValidateBundle(parsed);
                incomes = parsed.Incomes ?? new List<Income>();
                expenses = parsed.Expenses ?? new List<Expense>();
                subscriptions = parsed.Subscriptions ?? new List<Subscription>();
                Console.WriteLine("Dati importēti veiksmīgi.");
                Tools.Pause();
            }
            catch (JsonException jex)
            {
                Console.WriteLine("JSON kļūda: " + jex.Message);
                Console.WriteLine("Dati netika mainīti.");
                Tools.Pause();
            }
            catch (ValidationException vex)
            {
                Console.WriteLine("Validācijas kļūda: " + vex.Message);
                Console.WriteLine("Dati netika mainīti.");
                Tools.Pause();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Kļūda: " + ex.Message);
                Console.WriteLine("Dati netika mainīti.");
                Tools.Pause();
            }
        }

        static void ValidateBundle(DataBundle b)
        {
            if (b.Incomes != null)
            {
                foreach (var i in b.Incomes)
                {
                    if (i.Amount <= 0) throw new ValidationException("Import: Ienākuma summa jābūt > 0.");
                    if (i.Date == default) throw new ValidationException("Import: Ienākumam jābūt datums.");
                    if (string.IsNullOrWhiteSpace(i.Source)) throw new ValidationException("Import: Ienākuma avots nedrīkst būt tukšs.");
                }
            }
            if (b.Expenses != null)
            {
                foreach (var e in b.Expenses)
                {
                    if (e.Amount <= 0) throw new ValidationException("Import: Izdevuma summa jābūt > 0.");
                    if (e.Date == default) throw new ValidationException("Import: Izdevumam jābūt datums.");
                    // Category is enum; if invalid when deserialized will throw earlier; note can be empty
                }
            }
            if (b.Subscriptions != null)
            {
                foreach (var s in b.Subscriptions)
                {
                    if (string.IsNullOrWhiteSpace(s.Name)) throw new ValidationException("Import: Abonementam jābūt nosaukumam.");
                    if (s.MonthlyPrice <= 0) throw new ValidationException("Import: Abonementa cena jābūt > 0.");
                    if (s.StartDate == default) throw new ValidationException("Import: Abonementam jābūt starta datumam.");
                }
            }
        }
        #endregion

        static void ShowHelpShort()
        {
            Console.WriteLine("Personīgā finanšu plānotāja prototips. Lai sāktu, spiediet Enter.");
            Console.ReadLine();
        }

        static void ShowHelp()
        {
            Console.Clear();
            Console.WriteLine("--- Palīdzība ---");
            Console.WriteLine("- Ievades datumi formātā yyyy-MM-dd (piem., 2025-09-29)");
            Console.WriteLine("- Cenas un summas ievada ar decimālām zīmēm (piem., 123.45)");
            Console.WriteLine("- Kategorijas: Food, Transport, Fun, School, Other");
            Console.WriteLine("- Import/Export strādā ar JSON tekstu konsolē (failus nelieto)");
            Console.WriteLine("- Mēneša pārskats: ievadiet YYYY-MM (piem., 2025-09)");
            Tools.Pause();
        }
    }
}
