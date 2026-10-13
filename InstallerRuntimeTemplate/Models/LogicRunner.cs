
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows;
using SetupStudio.Models;

namespace SetupStudio
{
    public static class LogicRunner
    {
        public static async Task RunAsync(
            IReadOnlyList<LogicBlock> blocks,
            string? selectedStartId,
            Action<string> report,
            Window owner,
            string? selectedInstallDirectory = null,
            Action<string>? openPage = null)
        {
            if (blocks.Count == 0)
            {
                MessageBox.Show(
                    owner,
                    "Сначала добавь блоки на рабочее поле.",
                    "Тест логики",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var byId = blocks
                .Where(b => !string.IsNullOrWhiteSpace(b.Id))
                .GroupBy(b => b.Id)
                .ToDictionary(g => g.Key, g => g.First());

            LogicBlock? current = null;

            if (!string.IsNullOrWhiteSpace(selectedStartId))
                byId.TryGetValue(selectedStartId, out current);

            current ??= blocks.FirstOrDefault(b => b.Type == "Start");
            current ??= blocks.FirstOrDefault();

            var variables = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            int steps = 0;
            const int maxSteps = 500;

            report($"Запуск логики с блока «{current.Name}».");

            try
            {
                while (current != null)
                {
                    steps++;

                    if (steps > maxSteps)
                    {
                        report("Остановка: превышен лимит в 500 шагов. Проверь циклические связи.");
                        MessageBox.Show(
                            owner,
                            "Выполнение остановлено: логика выполнила более 500 шагов. Возможно, создан бесконечный цикл.",
                            "Тест логики",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    report($"Выполняется блок: {current.Name}");

                    string? nextId = current.NextId;

                    switch (current.Type)
                    {
                        case "Start":
                            break;

                        case "SlideOpened":
                            report($"Событие: открыт экран №{current.ScreenNumber}.");
                            break;

                        case "InstallProgram":
                            {
                                if (string.IsNullOrWhiteSpace(current.ProgramPath) || !File.Exists(current.ProgramPath))
                                {
                                    report("Установка пропущена: файл программы не найден в свойствах блока.");
                                    break;
                                }
                                string destination = !string.IsNullOrWhiteSpace(current.InstallDirectory)
                                    ? current.InstallDirectory
                                    : !string.IsNullOrWhiteSpace(selectedInstallDirectory)
                                        ? selectedInstallDirectory
                                        : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                                var answer = MessageBox.Show(owner,
                                    $"Запустить выбранный установщик?\n\nФайл: {current.ProgramPath}\nПапка назначения: {destination}\n\nЗапускай только программы, которым доверяешь.",
                                    "Подтверждение установки", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (answer == MessageBoxResult.Yes)
                                {
                                    var psi = new ProcessStartInfo(current.ProgramPath) { UseShellExecute = true, Arguments = $"/D=\"{destination}\"" };
                                    Process.Start(psi);
                                    report("Установщик запущен. Параметр /D поддерживается не всеми установщиками.");
                                }
                                break;
                            }

                        case "ButtonClick":
                            report($"Тестовое событие кнопки: {current.EventButton}. В тестовом режиме событие имитируется.");
                            break;

                        case "If":
                        case "Compare":
                            {
                                bool result = EvaluateCondition(current, variables);

                                report(
                                    $"Условие «{current.LeftOperand} {current.CompareOperator} {current.RightOperand}»: " +
                                    (result ? "ДА" : "НЕТ"));

                                // Ветвления ДА/НЕТ удалены: после проверки идём по обычной связи.
                                nextId = current.NextId;
                                break;
                            }

                        case "ShowMessage":
                            {
                                string message = ReplaceVariables(
                                    current.Text,
                                    variables);

                                MessageBox.Show(
                                    owner,
                                    message,
                                    current.Name,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                                break;
                            }

                        case "SetVariable":
                            {
                                if (string.IsNullOrWhiteSpace(current.VariableName))
                                {
                                    report("Переменная не изменена: укажи её имя.");
                                    break;
                                }

                                string value = ReplaceVariables(
                                    current.Value,
                                    variables);

                                variables[current.VariableName] = value;
                                report($"{current.VariableName} = {value}");
                                break;
                            }

                        case "OpenPage":
                            report($"Открывается экран: {current.PageName}.");
                            openPage?.Invoke(current.PageName);
                            break;

                        case "Wait":
                            {
                                int delay = Math.Clamp(
                                    current.WaitMilliseconds,
                                    0,
                                    60000);

                                await Task.Delay(delay);
                                break;
                            }

                        case "Repeat":
                            report(
                                $"Блок «Повторить»: задано {current.RepeatCount} повторений. " +
                                "Для полноценного цикла нужно настроить отдельную связь возврата.");
                            break;

                        case "Finish":
                            report("Выполнение завершено блоком «Конец».");
                            MessageBox.Show(
                                owner,
                                "Тест логики завершён.",
                                "KST Setup Creator",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            return;

                        default:
                            report($"Неизвестный тип блока: {current.Type}");
                            break;
                    }

                    if (string.IsNullOrWhiteSpace(nextId))
                    {
                        report("Выполнение завершено: следующий блок не подключён.");
                        break;
                    }

                    if (!byId.TryGetValue(nextId, out current))
                    {
                        report("Переход ведёт к отсутствующему блоку. Проверь соединения.");
                        break;
                    }
                }

                report("Тест завершён.");
            }
            catch (Exception ex)
            {
                report("Ошибка выполнения: " + ex.Message);

                MessageBox.Show(
                    owner,
                    $"Ошибка во время теста:\n{ex.Message}",
                    "Ошибка выполнения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static bool EvaluateCondition(
            LogicBlock block,
            Dictionary<string, string> variables)
        {
            string left = ResolveValue(block.LeftOperand, variables);
            string right = ResolveValue(block.RightOperand, variables);

            bool leftIsNumber = decimal.TryParse(
                left,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal leftNumber);

            bool rightIsNumber = decimal.TryParse(
                right,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal rightNumber);

            int comparison;

            if (leftIsNumber && rightIsNumber)
                comparison = leftNumber.CompareTo(rightNumber);
            else
                comparison = string.Compare(
                    left,
                    right,
                    StringComparison.OrdinalIgnoreCase);

            return block.CompareOperator switch
            {
                "равно" => string.Equals(
                    left, right, StringComparison.OrdinalIgnoreCase),

                "не равно" => !string.Equals(
                    left, right, StringComparison.OrdinalIgnoreCase),

                "больше" => leftIsNumber && rightIsNumber && comparison > 0,
                "меньше" => leftIsNumber && rightIsNumber && comparison < 0,

                "больше или равно" =>
                    leftIsNumber && rightIsNumber && comparison >= 0,

                "меньше или равно" =>
                    leftIsNumber && rightIsNumber && comparison <= 0,

                "содержит" => left.Contains(
                    right,
                    StringComparison.OrdinalIgnoreCase),

                _ => false
            };
        }

        private static string ResolveValue(
            string? input,
            Dictionary<string, string> variables)
        {
            string value = input ?? "";

            if (variables.TryGetValue(value, out string? variableValue))
                return variableValue;

            if (value.StartsWith("$", StringComparison.Ordinal) &&
                variables.TryGetValue(value.Substring(1), out variableValue))
            {
                return variableValue;
            }

            return value;
        }

        private static string ReplaceVariables(
            string? text,
            Dictionary<string, string> variables)
        {
            string result = text ?? "";

            foreach (var pair in variables)
            {
                result = result.Replace(
                    "${" + pair.Key + "}",
                    pair.Value,
                    StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }
    }
}
