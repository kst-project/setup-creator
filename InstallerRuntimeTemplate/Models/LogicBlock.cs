using System;

namespace SetupStudio.Models
{
    public class LogicBlock
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Новый блок";
        public string Category { get; set; } = "Действия";
        public string Type { get; set; } = "ShowMessage";
        public string SlideName { get; set; } = "Экран 1";
        public int ScreenNumber { get; set; } = 1;
        public string ProgramPath { get; set; } = "";
        public string InstallDirectory { get; set; } = "";

        public string Text { get; set; } = "Сообщение";
        public string EventButton { get; set; } = "";
        public string EventButtonId { get; set; } = "";

        public string VariableName { get; set; } = "myVariable";
        public string Value { get; set; } = "";

        public string ConditionType { get; set; } = "Переменная";
        public string CompareOperator { get; set; } = "равно";
        public string LeftOperand { get; set; } = "";
        public string RightOperand { get; set; } = "";

        public int RepeatCount { get; set; } = 3;
        public int WaitMilliseconds { get; set; } = 1000;
        public string PageName { get; set; } = "Главная";

        public double X { get; set; } = 40;
        public double Y { get; set; } = 40;

        // Все действия идут последовательно. Условия не имеют веток ДА/НЕТ.
        public string? NextId { get; set; }
    }
}
