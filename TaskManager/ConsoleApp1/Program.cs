using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace PersonalTaskManager
{
    // Data Models
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Priority Priority { get; set; } = Priority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        public TimeSpan? EstimatedTime { get; set; }
        public TimeSpan ActualTime { get; set; }
        public int Progress { get; set; } = 0; // 0-100
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class Note
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class PomodoroSession
    {
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int SessionsUntilLongBreak { get; set; } = 4;
        public int CompletedSessions { get; set; } = 0;
    }

    public enum Priority { Low = 1, Medium = 2, High = 3, Critical = 4 }
    public enum TaskStatus { Pending, InProgress, Completed, Cancelled }

    // Main Application Class
    public class TaskManager
    {
        private List<TaskItem> tasks = new List<TaskItem>();
        private List<Note> notes = new List<Note>();
        private PomodoroSession pomodoro = new PomodoroSession();
        private int nextTaskId = 1;
        private int nextNoteId = 1;
        private readonly string dataFile = "taskmanager_data.json";

        public void Run()
        {
            LoadData();
            ShowWelcome();

            while (true)
            {
                ShowMainMenu();
                var choice = Console.ReadLine();

                try
                {
                    switch (choice?.ToLower())
                    {
                        case "1": case "tasks": TaskManagement(); break;
                        case "2": case "notes": NotesManagement(); break;
                        case "3": case "timer": TimerMenu(); break;
                        case "4": case "pomodoro": PomodoroTimer(); break;
                        case "5": case "stats": ShowStatistics(); break;
                        case "6": case "export": ExportData(); break;
                        case "7": case "help": ShowHelp(); break;
                        case "8":
                        case "exit":
                        case "quit":
                            SaveData();
                            Console.WriteLine("Thanks for using Personal Task Manager! Goodbye!");
                            return;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        private void ShowWelcome()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              PERSONAL TASK MANAGER v2.0                  ║");
            Console.WriteLine("║                Stay Organized, Stay Productive           ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine($"\nWelcome! Today is {DateTime.Now:dddd, MMMM dd, yyyy}");
            Console.WriteLine($"You have {tasks.Count(t => t.Status != TaskStatus.Completed)} pending tasks.");
        }

        private void ShowMainMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n═══ MAIN MENU ═══");
            Console.ResetColor();
            Console.WriteLine("1. Task Management");
            Console.WriteLine("2. Notes Management");
            Console.WriteLine("3. Timer & Stopwatch");
            Console.WriteLine("4. Pomodoro Technique");
            Console.WriteLine("5. Statistics & Reports");
            Console.WriteLine("6. Export Data");
            Console.WriteLine("7. Help");
            Console.WriteLine("8. Exit");
            Console.Write("\nSelect an option (1-8 or type name): ");
        }

        #region Task Management
        private void TaskManagement()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("═══ TASK MANAGEMENT ═══");
                Console.ResetColor();
                Console.WriteLine("1. Add New Task");
                Console.WriteLine("2. View All Tasks");
                Console.WriteLine("3. Update Task");
                Console.WriteLine("4. Mark Task Complete");
                Console.WriteLine("5. Delete Task");
                Console.WriteLine("6. Search Tasks");
                Console.WriteLine("7. Filter Tasks");
                Console.WriteLine("8. Start Task Timer");
                Console.WriteLine("9. Back to Main Menu");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": AddTask(); break;
                    case "2": ViewTasks(); break;
                    case "3": UpdateTask(); break;
                    case "4": MarkTaskComplete(); break;
                    case "5": DeleteTask(); break;
                    case "6": SearchTasks(); break;
                    case "7": FilterTasks(); break;
                    case "8": StartTaskTimer(); break;
                    case "9": return;
                    default: Console.WriteLine("Invalid option."); break;
                }

                if (choice != "9")
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private void AddTask()
        {
            Console.Clear();
            Console.WriteLine("═══ ADD NEW TASK ═══");

            var task = new TaskItem { Id = nextTaskId++ };

            Console.Write("Task Title: ");
            task.Title = Console.ReadLine() ?? "";

            Console.Write("Description (optional): ");
            task.Description = Console.ReadLine() ?? "";

            Console.WriteLine("Priority (1=Low, 2=Medium, 3=High, 4=Critical): ");
            if (int.TryParse(Console.ReadLine(), out int priority) && priority >= 1 && priority <= 4)
                task.Priority = (Priority)priority;

            Console.Write("Due Date (yyyy-mm-dd, or press Enter to skip): ");
            var dueDateStr = Console.ReadLine();
            if (DateTime.TryParse(dueDateStr, out DateTime dueDate))
                task.DueDate = dueDate;

            Console.Write("Estimated time in hours (or press Enter to skip): ");
            if (double.TryParse(Console.ReadLine(), out double hours))
                task.EstimatedTime = TimeSpan.FromHours(hours);

            Console.Write("Tags (comma-separated, optional): ");
            var tagsStr = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(tagsStr))
                task.Tags = tagsStr.Split(',').Select(t => t.Trim()).ToList();

            tasks.Add(task);
            Console.WriteLine($"\n✓ Task '{task.Title}' added successfully!");
        }

        private void ViewTasks()
        {
            Console.Clear();
            Console.WriteLine("═══ ALL TASKS ═══\n");

            if (!tasks.Any())
            {
                Console.WriteLine("No tasks found. Add some tasks to get started!");
                return;
            }

            foreach (var task in tasks.OrderBy(t => t.Priority).ThenBy(t => t.DueDate))
            {
                DisplayTask(task);
                Console.WriteLine();
            }
        }

        private void DisplayTask(TaskItem task)
        {
            var priorityColor = task.Priority switch
            {
                Priority.Critical => ConsoleColor.Red,
                Priority.High => ConsoleColor.Yellow,
                Priority.Medium => ConsoleColor.White,
                Priority.Low => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = priorityColor;
            Console.Write($"[{task.Id}] {task.Title}");
            Console.ResetColor();

            Console.WriteLine($" ({task.Status})");
            if (!string.IsNullOrEmpty(task.Description))
                Console.WriteLine($"    Description: {task.Description}");

            Console.WriteLine($"    Priority: {task.Priority} | Progress: {DrawProgressBar(task.Progress)}");

            if (task.DueDate.HasValue)
            {
                var daysUntilDue = (task.DueDate.Value - DateTime.Now).Days;
                Console.Write("    Due: " + task.DueDate.Value.ToString("yyyy-MM-dd"));
                if (daysUntilDue < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" (OVERDUE by {Math.Abs(daysUntilDue)} days)");
                }
                else if (daysUntilDue <= 3)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($" (Due in {daysUntilDue} days)");
                }
                else
                {
                    Console.WriteLine($" (Due in {daysUntilDue} days)");
                }
                Console.ResetColor();
            }

            if (task.Tags.Any())
                Console.WriteLine($"    Tags: {string.Join(", ", task.Tags)}");

            if (task.ActualTime.TotalMinutes > 0)
                Console.WriteLine($"    Time spent: {task.ActualTime:hh\\:mm\\:ss}");
        }

        private string DrawProgressBar(int progress)
        {
            const int barLength = 20;
            int filledLength = (int)(barLength * progress / 100.0);
            string bar = new string('█', filledLength) + new string('░', barLength - filledLength);
            return $"[{bar}] {progress}%";
        }

        private void UpdateTask()
        {
            Console.Write("Enter task ID to update: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var task = tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    Console.WriteLine($"Updating task: {task.Title}");
                    Console.Write($"New progress (0-100, current: {task.Progress}): ");
                    if (int.TryParse(Console.ReadLine(), out int progress) && progress >= 0 && progress <= 100)
                    {
                        task.Progress = progress;
                        if (progress == 100)
                            task.Status = TaskStatus.Completed;
                        else if (progress > 0)
                            task.Status = TaskStatus.InProgress;

                        Console.WriteLine("✓ Task updated successfully!");
                    }
                }
                else
                {
                    Console.WriteLine("Task not found.");
                }
            }
        }

        private void MarkTaskComplete()
        {
            Console.Write("Enter task ID to mark complete: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var task = tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    task.Status = TaskStatus.Completed;
                    task.Progress = 100;
                    Console.WriteLine($"✓ Task '{task.Title}' marked as completed!");
                }
                else
                {
                    Console.WriteLine("Task not found.");
                }
            }
        }

        private void DeleteTask()
        {
            Console.Write("Enter task ID to delete: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var task = tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    Console.Write($"Are you sure you want to delete '{task.Title}'? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        tasks.Remove(task);
                        Console.WriteLine("✓ Task deleted successfully!");
                    }
                }
                else
                {
                    Console.WriteLine("Task not found.");
                }
            }
        }

        private void SearchTasks()
        {
            Console.Write("Enter search term: ");
            var searchTerm = Console.ReadLine()?.ToLower();
            if (string.IsNullOrWhiteSpace(searchTerm)) return;

            var matchingTasks = tasks.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                t.Description.ToLower().Contains(searchTerm) ||
                t.Tags.Any(tag => tag.ToLower().Contains(searchTerm))
            ).ToList();

            Console.WriteLine($"\n═══ SEARCH RESULTS for '{searchTerm}' ═══");
            if (matchingTasks.Any())
            {
                foreach (var task in matchingTasks)
                {
                    DisplayTask(task);
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No matching tasks found.");
            }
        }

        private void FilterTasks()
        {
            Console.WriteLine("Filter by:");
            Console.WriteLine("1. Status");
            Console.WriteLine("2. Priority");
            Console.WriteLine("3. Due Date");
            Console.Write("Select filter: ");

            var choice = Console.ReadLine();
            List<TaskItem> filteredTasks = new List<TaskItem>();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("Status: 1=Pending, 2=InProgress, 3=Completed, 4=Cancelled");
                    if (int.TryParse(Console.ReadLine(), out int status) && status >= 1 && status <= 4)
                    {
                        filteredTasks = tasks.Where(t => (int)t.Status == status - 1).ToList();
                    }
                    break;
                case "2":
                    Console.WriteLine("Priority: 1=Low, 2=Medium, 3=High, 4=Critical");
                    if (int.TryParse(Console.ReadLine(), out int priority) && priority >= 1 && priority <= 4)
                    {
                        filteredTasks = tasks.Where(t => (int)t.Priority == priority).ToList();
                    }
                    break;
                case "3":
                    filteredTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date <= DateTime.Now.Date.AddDays(7)).ToList();
                    break;
            }

            Console.WriteLine("\n═══ FILTERED TASKS ═══");
            foreach (var task in filteredTasks)
            {
                DisplayTask(task);
                Console.WriteLine();
            }
        }

        private void StartTaskTimer()
        {
            Console.Write("Enter task ID to start timer: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var task = tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    RunTaskTimer(task);
                }
                else
                {
                    Console.WriteLine("Task not found.");
                }
            }
        }

        private void RunTaskTimer(TaskItem task)
        {
            Console.Clear();
            Console.WriteLine($"═══ TIMER: {task.Title} ═══");
            Console.WriteLine("Press 'q' to quit timer\n");

            var startTime = DateTime.Now;
            task.Status = TaskStatus.InProgress;

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        break;
                }

                var elapsed = DateTime.Now - startTime;
                Console.SetCursorPosition(0, 3);
                Console.WriteLine($"Time: {elapsed:hh\\:mm\\:ss}");

                Thread.Sleep(1000);
            }

            var totalElapsed = DateTime.Now - startTime;
            task.ActualTime = task.ActualTime.Add(totalElapsed);
            Console.WriteLine($"\nTimer stopped. Total time for this session: {totalElapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"Total time on task: {task.ActualTime:hh\\:mm\\:ss}");
        }
        #endregion

        #region Notes Management
        private void NotesManagement()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("═══ NOTES MANAGEMENT ═══");
                Console.ResetColor();
                Console.WriteLine("1. Add New Note");
                Console.WriteLine("2. View All Notes");
                Console.WriteLine("3. Edit Note");
                Console.WriteLine("4. Search Notes");
                Console.WriteLine("5. Delete Note");
                Console.WriteLine("6. Back to Main Menu");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": AddNote(); break;
                    case "2": ViewNotes(); break;
                    case "3": EditNote(); break;
                    case "4": SearchNotes(); break;
                    case "5": DeleteNote(); break;
                    case "6": return;
                    default: Console.WriteLine("Invalid option."); break;
                }

                if (choice != "6")
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private void AddNote()
        {
            Console.Clear();
            Console.WriteLine("═══ ADD NEW NOTE ═══");

            var note = new Note { Id = nextNoteId++ };

            Console.Write("Note Title: ");
            note.Title = Console.ReadLine() ?? "";

            Console.WriteLine("Note Content (type 'END' on a new line to finish):");
            var content = new List<string>();
            string line;
            while ((line = Console.ReadLine()) != "END")
            {
                content.Add(line);
            }
            note.Content = string.Join(Environment.NewLine, content);

            Console.Write("Tags (comma-separated, optional): ");
            var tagsStr = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(tagsStr))
                note.Tags = tagsStr.Split(',').Select(t => t.Trim()).ToList();

            notes.Add(note);
            Console.WriteLine($"\n✓ Note '{note.Title}' added successfully!");
        }

        private void ViewNotes()
        {
            Console.Clear();
            Console.WriteLine("═══ ALL NOTES ═══\n");

            if (!notes.Any())
            {
                Console.WriteLine("No notes found. Add some notes to get started!");
                return;
            }

            foreach (var note in notes.OrderByDescending(n => n.CreatedDate))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[{note.Id}] {note.Title}");
                Console.ResetColor();
                Console.WriteLine($"Created: {note.CreatedDate:yyyy-MM-dd HH:mm}");
                if (note.ModifiedDate.HasValue)
                    Console.WriteLine($"Modified: {note.ModifiedDate:yyyy-MM-dd HH:mm}");

                if (note.Tags.Any())
                    Console.WriteLine($"Tags: {string.Join(", ", note.Tags)}");

                var preview = note.Content.Length > 100 ? note.Content.Substring(0, 100) + "..." : note.Content;
                Console.WriteLine($"Preview: {preview}");
                Console.WriteLine(new string('-', 50));
            }
        }

        private void EditNote()
        {
            Console.Write("Enter note ID to edit: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var note = notes.FirstOrDefault(n => n.Id == id);
                if (note != null)
                {
                    Console.WriteLine($"Editing note: {note.Title}");
                    Console.WriteLine("Current content:");
                    Console.WriteLine(note.Content);
                    Console.WriteLine("\nNew content (type 'END' on a new line to finish):");

                    var content = new List<string>();
                    string line;
                    while ((line = Console.ReadLine()) != "END")
                    {
                        content.Add(line);
                    }
                    note.Content = string.Join(Environment.NewLine, content);
                    note.ModifiedDate = DateTime.Now;

                    Console.WriteLine("✓ Note updated successfully!");
                }
                else
                {
                    Console.WriteLine("Note not found.");
                }
            }
        }

        private void SearchNotes()
        {
            Console.Write("Enter search term: ");
            var searchTerm = Console.ReadLine()?.ToLower();
            if (string.IsNullOrWhiteSpace(searchTerm)) return;

            var matchingNotes = notes.Where(n =>
                n.Title.ToLower().Contains(searchTerm) ||
                n.Content.ToLower().Contains(searchTerm) ||
                n.Tags.Any(tag => tag.ToLower().Contains(searchTerm))
            ).ToList();

            Console.WriteLine($"\n═══ SEARCH RESULTS for '{searchTerm}' ═══");
            foreach (var note in matchingNotes)
            {
                Console.WriteLine($"[{note.Id}] {note.Title}");
                Console.WriteLine($"Preview: {(note.Content.Length > 100 ? note.Content.Substring(0, 100) + "..." : note.Content)}");
                Console.WriteLine();
            }
        }

        private void DeleteNote()
        {
            Console.Write("Enter note ID to delete: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var note = notes.FirstOrDefault(n => n.Id == id);
                if (note != null)
                {
                    Console.Write($"Are you sure you want to delete '{note.Title}'? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        notes.Remove(note);
                        Console.WriteLine("✓ Note deleted successfully!");
                    }
                }
                else
                {
                    Console.WriteLine("Note not found.");
                }
            }
        }
        #endregion

        #region Timer and Pomodoro
        private void TimerMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("═══ TIMER & STOPWATCH ═══");
            Console.ResetColor();
            Console.WriteLine("1. Countdown Timer");
            Console.WriteLine("2. Stopwatch");
            Console.WriteLine("3. Back to Main Menu");
            Console.Write("\nSelect an option: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1": CountdownTimer(); break;
                case "2": Stopwatch(); break;
                case "3": return;
            }
        }

        private void CountdownTimer()
        {
            Console.Write("Enter minutes for countdown: ");
            if (int.TryParse(Console.ReadLine(), out int minutes))
            {
                var totalSeconds = minutes * 60;
                Console.Clear();
                Console.WriteLine($"═══ COUNTDOWN: {minutes} MINUTES ═══");
                Console.WriteLine("Press any key to stop");

                while (totalSeconds > 0)
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        break;
                    }

                    var mins = totalSeconds / 60;
                    var secs = totalSeconds % 60;

                    Console.SetCursorPosition(0, 3);
                    Console.WriteLine($"Time remaining: {mins:D2}:{secs:D2}");

                    Thread.Sleep(1000);
                    totalSeconds--;
                }

                if (totalSeconds <= 0)
                {
                    Console.WriteLine("\n🔔 TIME'S UP! 🔔");
                    Console.Beep();
                }
            }
        }

        private void Stopwatch()
        {
            Console.Clear();
            Console.WriteLine("═══ STOPWATCH ═══");
            Console.WriteLine("Press any key to stop");

            var startTime = DateTime.Now;
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    break;
                }

                var elapsed = DateTime.Now - startTime;
                Console.SetCursorPosition(0, 3);
                Console.WriteLine($"Elapsed: {elapsed:hh\\:mm\\:ss}");

                Thread.Sleep(1000);
            }

            var finalTime = DateTime.Now - startTime;
            Console.WriteLine($"\nFinal time: {finalTime:hh\\:mm\\:ss}");
        }

        private void PomodoroTimer()
        {
            Console.Clear();
            Console.WriteLine("═══ POMODORO TECHNIQUE ═══");
            Console.WriteLine($"Work: {pomodoro.WorkMinutes} min | Short Break: {pomodoro.ShortBreakMinutes} min | Long Break: {pomodoro.LongBreakMinutes} min");
            Console.WriteLine($"Sessions completed today: {pomodoro.CompletedSessions}");
            Console.WriteLine("\n1. Start Pomodoro Session");
            Console.WriteLine("2. Configure Settings");
            Console.WriteLine("3. Back to Main Menu");
            Console.Write("\nSelect an option: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1": StartPomodoroSession(); break;
                case "2": ConfigurePomodoro(); break;
                case "3": return;
            }
        }

        private void StartPomodoroSession()
        {
            bool isWorkSession = true;
            int sessionCount = 0;

            while (true)
            {
                int minutes = isWorkSession ? pomodoro.WorkMinutes :
                             (sessionCount % pomodoro.SessionsUntilLongBreak == 0 && sessionCount > 0) ?
                             pomodoro.LongBreakMinutes : pomodoro.ShortBreakMinutes;

                string sessionType = isWorkSession ? "WORK" :
                                   (sessionCount % pomodoro.SessionsUntilLongBreak == 0 && sessionCount > 0) ?
                                   "LONG BREAK" : "SHORT BREAK";

                Console.Clear();
                Console.ForegroundColor = isWorkSession ? ConsoleColor.Green : ConsoleColor.Blue;
                Console.WriteLine($"═══ POMODORO: {sessionType} ({minutes} min) ═══");
                Console.ResetColor();
                Console.WriteLine("Press 'q' to quit, 's' to skip session");

                if (RunPomodoroTimer(minutes))
                {
                    if (isWorkSession)
                    {
                        pomodoro.CompletedSessions++;
                        sessionCount++;
                    }

                    Console.WriteLine($"\n🔔 {sessionType} SESSION COMPLETE! 🔔");
                    Console.Beep();
                    isWorkSession = !isWorkSession;

                    Console.WriteLine("\nPress any key to continue or 'q' to quit...");
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        break;
                }
                else
                {
                    break;
                }
            }
        }

        private bool RunPomodoroTimer(int minutes)
        {
            var totalSeconds = minutes * 60;

            while (totalSeconds > 0)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        return false;
                    if (key.KeyChar == 's' || key.KeyChar == 'S')
                        return true;
                }

                var mins = totalSeconds / 60;
                var secs = totalSeconds % 60;

                Console.SetCursorPosition(0, 4);
                Console.WriteLine($"Time remaining: {mins:D2}:{secs:D2}");
                Console.WriteLine(DrawProgressBar(100 - (totalSeconds * 100 / (minutes * 60))));

                Thread.Sleep(1000);
                totalSeconds--;
            }

            return true;
        }

        private void ConfigurePomodoro()
        {
            Console.WriteLine("Current settings:");
            Console.WriteLine($"Work session: {pomodoro.WorkMinutes} minutes");
            Console.WriteLine($"Short break: {pomodoro.ShortBreakMinutes} minutes");
            Console.WriteLine($"Long break: {pomodoro.LongBreakMinutes} minutes");
            Console.WriteLine($"Sessions until long break: {pomodoro.SessionsUntilLongBreak}");

            Console.Write("\nEnter new work session minutes (or press Enter to keep current): ");
            var input = Console.ReadLine();
            if (int.TryParse(input, out int workMin) && workMin > 0)
                pomodoro.WorkMinutes = workMin;

            Console.Write("Enter new short break minutes (or press Enter to keep current): ");
            input = Console.ReadLine();
            if (int.TryParse(input, out int shortBreak) && shortBreak > 0)
                pomodoro.ShortBreakMinutes = shortBreak;

            Console.Write("Enter new long break minutes (or press Enter to keep current): ");
            input = Console.ReadLine();
            if (int.TryParse(input, out int longBreak) && longBreak > 0)
                pomodoro.LongBreakMinutes = longBreak;

            Console.WriteLine("\n✓ Pomodoro settings updated!");
        }
        #endregion

        #region Statistics and Reports
        private void ShowStatistics()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══ STATISTICS & REPORTS ═══");
            Console.ResetColor();

            // Task Statistics
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == TaskStatus.Completed);
            var pendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending);
            var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
            var overdueTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate < DateTime.Now && t.Status != TaskStatus.Completed);

            Console.WriteLine("📊 TASK OVERVIEW");
            Console.WriteLine($"Total Tasks: {totalTasks}");
            Console.WriteLine($"Completed: {completedTasks} ({(totalTasks > 0 ? completedTasks * 100 / totalTasks : 0)}%)");
            Console.WriteLine($"In Progress: {inProgressTasks}");
            Console.WriteLine($"Pending: {pendingTasks}");
            Console.WriteLine($"Overdue: {overdueTasks}");

            if (totalTasks > 0)
            {
                Console.WriteLine($"\nCompletion Rate: {DrawProgressBar(completedTasks * 100 / totalTasks)}");
            }

            // Priority Distribution
            Console.WriteLine("\n📈 PRIORITY DISTRIBUTION");
            foreach (Priority priority in Enum.GetValues<Priority>())
            {
                var count = tasks.Count(t => t.Priority == priority);
                Console.WriteLine($"{priority}: {count} tasks");
            }

            // Time Statistics
            var totalTimeSpent = tasks.Sum(t => t.ActualTime.TotalHours);
            var avgTimePerTask = totalTasks > 0 ? totalTimeSpent / totalTasks : 0;

            Console.WriteLine("\n⏱️ TIME TRACKING");
            Console.WriteLine($"Total time logged: {totalTimeSpent:F1} hours");
            Console.WriteLine($"Average time per task: {avgTimePerTask:F1} hours");
            Console.WriteLine($"Pomodoro sessions completed: {pomodoro.CompletedSessions}");

            // Upcoming Tasks
            var upcomingTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate > DateTime.Now && t.Status != TaskStatus.Completed)
                                    .OrderBy(t => t.DueDate)
                                    .Take(5)
                                    .ToList();

            if (upcomingTasks.Any())
            {
                Console.WriteLine("\n📅 UPCOMING TASKS (Next 5)");
                foreach (var task in upcomingTasks)
                {
                    var daysUntil = (task.DueDate!.Value - DateTime.Now).Days;
                    Console.WriteLine($"• {task.Title} - Due in {daysUntil} day(s)");
                }
            }

            // Notes Statistics
            Console.WriteLine($"\n📝 NOTES");
            Console.WriteLine($"Total Notes: {notes.Count}");
            Console.WriteLine($"Total Characters: {notes.Sum(n => n.Content.Length):N0}");
        }

        private void ExportData()
        {
            Console.Clear();
            Console.WriteLine("═══ EXPORT DATA ═══");
            Console.WriteLine("1. Export Tasks to CSV");
            Console.WriteLine("2. Export Notes to Text File");
            Console.WriteLine("3. Export All Data to JSON");
            Console.WriteLine("4. Back to Main Menu");
            Console.Write("\nSelect export option: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1": ExportTasksToCSV(); break;
                case "2": ExportNotesToText(); break;
                case "3": ExportAllToJSON(); break;
                case "4": return;
            }
        }

        private void ExportTasksToCSV()
        {
            try
            {
                var fileName = $"tasks_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine("ID,Title,Description,Priority,Status,Progress,DueDate,EstimatedHours,ActualHours,Tags");
                    foreach (var task in tasks)
                    {
                        var line = $"{task.Id}," +
                                  $"\"{task.Title}\"," +
                                  $"\"{task.Description}\"," +
                                  $"{task.Priority}," +
                                  $"{task.Status}," +
                                  $"{task.Progress}," +
                                  $"{task.DueDate?.ToString("yyyy-MM-dd") ?? ""}," +
                                  $"{task.EstimatedTime?.TotalHours ?? 0:F2}," +
                                  $"{task.ActualTime.TotalHours:F2}," +
                                  $"\"{string.Join(";", task.Tags)}\"";
                        writer.WriteLine(line);
                    }
                }
                Console.WriteLine($"✓ Tasks exported to {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting tasks: {ex.Message}");
            }
        }

        private void ExportNotesToText()
        {
            try
            {
                var fileName = $"notes_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine("PERSONAL TASK MANAGER - NOTES EXPORT");
                    writer.WriteLine($"Exported on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine();

                    foreach (var note in notes.OrderByDescending(n => n.CreatedDate))
                    {
                        writer.WriteLine($"[{note.Id}] {note.Title}");
                        writer.WriteLine($"Created: {note.CreatedDate:yyyy-MM-dd HH:mm}");
                        if (note.ModifiedDate.HasValue)
                            writer.WriteLine($"Modified: {note.ModifiedDate:yyyy-MM-dd HH:mm}");
                        if (note.Tags.Any())
                            writer.WriteLine($"Tags: {string.Join(", ", note.Tags)}");
                        writer.WriteLine();
                        writer.WriteLine(note.Content);
                        writer.WriteLine();
                        writer.WriteLine(new string('-', 30));
                        writer.WriteLine();
                    }
                }
                Console.WriteLine($"✓ Notes exported to {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting notes: {ex.Message}");
            }
        }

        private void ExportAllToJSON()
        {
            try
            {
                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    Tasks = tasks,
                    Notes = notes,
                    PomodoroStats = pomodoro
                };

                var fileName = $"taskmanager_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(fileName, json);
                Console.WriteLine($"✓ All data exported to {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting data: {ex.Message}");
            }
        }
        #endregion

        #region Data Management
        private void SaveData()
        {
            try
            {
                var data = new
                {
                    Tasks = tasks,
                    Notes = notes,
                    Pomodoro = pomodoro,
                    NextTaskId = nextTaskId,
                    NextNoteId = nextNoteId,
                    LastSaved = DateTime.Now
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dataFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(dataFile))
                {
                    var json = File.ReadAllText(dataFile);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("Tasks", out JsonElement tasksElement))
                    {
                        tasks = JsonSerializer.Deserialize<List<TaskItem>>(tasksElement.GetRawText()) ?? new List<TaskItem>();
                    }

                    if (root.TryGetProperty("Notes", out JsonElement notesElement))
                    {
                        notes = JsonSerializer.Deserialize<List<Note>>(notesElement.GetRawText()) ?? new List<Note>();
                    }

                    if (root.TryGetProperty("Pomodoro", out JsonElement pomodoroElement))
                    {
                        pomodoro = JsonSerializer.Deserialize<PomodoroSession>(pomodoroElement.GetRawText()) ?? new PomodoroSession();
                    }

                    if (root.TryGetProperty("NextTaskId", out JsonElement taskIdElement))
                    {
                        nextTaskId = taskIdElement.GetInt32();
                    }

                    if (root.TryGetProperty("NextNoteId", out JsonElement noteIdElement))
                    {
                        nextNoteId = noteIdElement.GetInt32();
                    }

                    // Ensure IDs are correct
                    if (tasks.Any())
                        nextTaskId = Math.Max(nextTaskId, tasks.Max(t => t.Id) + 1);
                    if (notes.Any())
                        nextNoteId = Math.Max(nextNoteId, notes.Max(n => n.Id) + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                Console.WriteLine("Starting with fresh data...");
            }
        }
        #endregion

        #region Help System
        private void ShowHelp()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══ HELP & GUIDE ═══");
            Console.ResetColor();

            Console.WriteLine("\n🎯 TASK MANAGEMENT");
            Console.WriteLine("• Create tasks with priorities, due dates, and time estimates");
            Console.WriteLine("• Track progress with percentage completion");
            Console.WriteLine("• Use tags to organize and categorize tasks");
            Console.WriteLine("• Start timers to track actual time spent");
            Console.WriteLine("• Filter and search tasks by various criteria");

            Console.WriteLine("\n📝 NOTES");
            Console.WriteLine("• Create quick notes and longer documents");
            Console.WriteLine("• Use tags to organize notes by topic");
            Console.WriteLine("• Search through all notes by content or title");
            Console.WriteLine("• Edit existing notes anytime");

            Console.WriteLine("\n⏰ TIME MANAGEMENT");
            Console.WriteLine("• Use built-in timers and stopwatch");
            Console.WriteLine("• Pomodoro Technique: 25-min work + 5-min break cycles");
            Console.WriteLine("• Track time spent on individual tasks");
            Console.WriteLine("• Customize Pomodoro settings to your preference");

            Console.WriteLine("\n📊 ANALYTICS");
            Console.WriteLine("• View completion rates and productivity stats");
            Console.WriteLine("• See upcoming deadlines and overdue tasks");
            Console.WriteLine("• Track total time invested in tasks");

            Console.WriteLine("\n💾 DATA MANAGEMENT");
            Console.WriteLine("• All data is automatically saved");
            Console.WriteLine("• Export tasks to CSV for external analysis");
            Console.WriteLine("• Export notes to text files");
            Console.WriteLine("• Create JSON backups of all data");

            Console.WriteLine("\n🔧 TIPS FOR BEST RESULTS");
            Console.WriteLine("• Set realistic due dates and time estimates");
            Console.WriteLine("• Use tags consistently for better organization");
            Console.WriteLine("• Review your statistics regularly");
            Console.WriteLine("• Break large tasks into smaller, manageable pieces");
            Console.WriteLine("• Use the Pomodoro technique for focused work sessions");

            Console.WriteLine("\n🆘 KEYBOARD SHORTCUTS");
            Console.WriteLine("• In timers: 'q' to quit, 's' to skip (Pomodoro)");
            Console.WriteLine("• In menus: Type numbers or command names");
            Console.WriteLine("• Press any key to continue from result screens");
        }
        #endregion
    }

    // Program Entry Point
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "Personal Task Manager v2.0";
                Console.SetWindowSize(Math.Min(120, Console.LargestWindowWidth), Math.Min(40, Console.LargestWindowHeight));

                var taskManager = new TaskManager();
                taskManager.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}