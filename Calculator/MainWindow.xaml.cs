using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace Calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Exception ValueTooLongException = new ();

        private enum Operation
        {
            Add,
            Subtract,
            Divide,
            None
        }

        private enum Register
        {
            A,
            B
        }

        private static bool IsShowingResult;
        private static bool RegisterASet;
        private static bool RegisterBSet;
        private static double RegisterA;
        private static double RegisterB;

        private static Operation CurrentOperation;

        private static double OutputBuffer = 0;
        private static string InputBuffer = "";
        private static readonly bool InputBufferExceedsMaxLength = InputBuffer.Length > 8;
        private static readonly bool OutputBufferExceedsMaxLength = OutputBuffer.ToString(CultureInfo.CurrentCulture).Length > 8;

        /// <summary>
        /// Main window constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Helper Functions

        /// <summary>
        /// Clears the result and hides it from the result text.
        /// </summary>
        private void ClearResult()
        {
            Debug.WriteLineIf(!IsShowingResult, "Redundant ClearResult");

            // We need to set this flag to 'false' so we know that we are no longer displaying the result
            IsShowingResult = false;
            SetOutputTextToInputBuffer();
        }

        /// <summary>
        /// Sets the register to value, then flags the register as set.
        /// </summary>
        /// <param name="value">The value to set the register to.</param>
        /// <param name="register">The register to set the value to.</param>
        private static void SetRegister(double value, Register register)
        {
            var valueIsTooLong = value.ToString(CultureInfo.CurrentCulture).Length > 8;

            if (valueIsTooLong)
            {
                throw ValueTooLongException;
            }

            Debug.WriteLine($"{nameof(SetRegister)}({value}, {register})");

            switch (register)
            {
                case Register.A:
                    RegisterA = value;
                    RegisterASet = true;
                    break;

                case Register.B:
                    RegisterB = value;
                    RegisterBSet = true;
                    break;

                default:
                    throw new NotSupportedException("Only two registers (A and B) are supported.");
            }
        }

        /// <summary>
        /// Calls ClearRegister on both registers (A and B)
        /// </summary>
        private static void ClearRegisters()
        {
            Debug.WriteLineIf(!RegisterASet && !RegisterBSet, "Redundant ClearRegisters");

            ClearRegister(Register.A);
            ClearRegister(Register.B);
        }

        /// <summary>
        /// Clears the specified register
        /// </summary>
        /// <param name="register">The register to be cleared</param>
        /// <exception cref="ArgumentOutOfRangeException">Only two registers (A & B) are supported</exception>
        private static void ClearRegister(Register register)
        {
            switch (register)
            {
                case Register.A:
                    Debug.WriteLineIf(RegisterA == 0 || !RegisterASet, $"Redundant ClearRegister on {nameof(RegisterA)}");

                    RegisterA = 0;
                    RegisterASet = false;
                    break;

                case Register.B:
                    Debug.WriteLineIf(RegisterB == 0 || !RegisterBSet, $"Redundant ClearRegister on {nameof(RegisterB)}");

                    RegisterB = 0;
                    RegisterBSet = false;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(register), register, null);
            }
        }

        /// <summary>
        /// Resets the input buffer
        /// </summary>
        private static void ClearInputBuffer() => InputBuffer = "0";

        /// <summary>
        /// Resets the output buffer
        /// </summary>
        public static void ClearOutputBuffer() => OutputBuffer = 0;

        /// <summary>
        /// Sets 'BufferText.Text' to 'InputBuffer'
        /// </summary>
        private void SetOutputTextToInputBuffer()
        {
            Debug.WriteLineIf(
                BufferText.Text ==
                InputBuffer,
                $"Redundant {nameof(SetOutputTextToInputBuffer)}");

            Debug.Assert(
                !InputBufferExceedsMaxLength, nameof(OutputBufferExceedsMaxLength));

            BufferText.Text = InputBuffer;
        }

        /// <summary>
        /// Sets 'BufferText.Text' to 'string(OutputBuffer)'
        /// </summary>
        private void SetOutputTextToOutputBuffer()
        {
            Debug.WriteLineIf(
                BufferText.Text == OutputBuffer
                    .ToString(CultureInfo.CurrentCulture),
                $"Redundant {nameof(SetOutputTextToInputBuffer)}");

            Debug.Assert(
                !OutputBufferExceedsMaxLength, nameof(OutputBufferExceedsMaxLength));

            BufferText.Text = OutputBuffer.ToString(CultureInfo.CurrentCulture);
            IsShowingResult = true;
        }

        /// <summary>
        /// Triggers whenever a digit is entered.
        /// </summary>
        /// <param name="digit">The value of the digit. (Should not exceed base 10)</param>
        private void DigitPressed(double digit)
        {
            // If the input buffer is just 0, replace it with the digit
            if (InputBuffer == "0")
            {
                InputBuffer = "";
            }

            // Clearing the result will make it clear that the output being shown was a result
            if (IsShowingResult)
                ClearResult();

            // Limit the amount of digits to 8 so we don't have to do any shenanigans
            if (InputBuffer.ToString(CultureInfo.InvariantCulture).Length + 1 > 8)
                return;

            InputBuffer += digit;
            SetOutputTextToInputBuffer();
        }

        #endregion

        /// <summary>
        /// First sets the value of either <see cref="RegisterA"/> or <see cref="RegisterB"/>, then
        /// sets the current operation to the supplied <see cref="Operation"/>.
        /// </summary>
        /// <param name="newOperation">The operation to set <see cref="CurrentOperation"/> to.</param>
        private void SetOperation(Operation newOperation)
        {
            // We set the value of Register A if it is not set
            if (RegisterASet == false)
            {
                // Ignore empty operations
                if (InputBuffer is "")
                {
                    Debug.WriteLine("SetOperation aborted: InputBuffer is empty");
                    return;
                }

                try
                {
                    Debug.WriteLine($"Register A = {double.Parse(InputBuffer)}");

                    SetRegister(double.Parse(InputBuffer), Register.A);
                }
                catch (FormatException){} // Non-fatal Bug caused by pressing the clear button; cause unknown
            }

            // ReSharper disable once InvertIf (for consistency)
            if (RegisterBSet == false)
            {

                Debug.WriteLine("Operation = " + newOperation);
                CurrentOperation = newOperation;
                BufferText.Text = $"{CurrentOperation}({RegisterA}, ?)";
                ClearInputBuffer();
            }
        }

        /// <summary>
        /// Clears the input and output buffers, as well as clearing the memory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClearAll(object sender, RoutedEventArgs e)
        {
            ClearInputBuffer();
            ClearOutputBuffer();
            ClearRegisters();
            SetOutputTextToInputBuffer();

            CurrentOperation = Operation.None;
        }

        /// <summary>
        /// Clears the input buffer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear(object sender, RoutedEventArgs e)
        {
            if (IsShowingResult)
            {
                ClearResult();

                // We assume the user is meaning to clear the result from the screen and memory
                Debug.WriteLine("Hiding result + resetting register A");
                RegisterA = 0;
                RegisterASet = false;
            }

            // If we are currently at the operation selection stage, we clear the operation and show the previous value
            if (CurrentOperation != Operation.None)
            {
                CurrentOperation = Operation.None;
                InputBuffer = RegisterA.ToString(CultureInfo.CurrentCulture);
            }
            else // Otherwise, we just clear the input buffer
            {
                ClearInputBuffer();
            }

            SetOutputTextToInputBuffer();
        }

        /// <summary>
        /// Performs a calculation using the set registers and the set operation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void Calculate()
        {
            if (!RegisterBSet)
            {
                if (InputBuffer is "0" or "")
                    return;

                Debug.WriteLine("Register B = " + double.Parse(InputBuffer));
                RegisterB = double.Parse(InputBuffer);
                RegisterBSet = true;
            }

            Debug.WriteLine($"Calculate: {CurrentOperation}({RegisterA}, {RegisterB})");
            switch (CurrentOperation)
            {
                case Operation.Add:
                    OutputBuffer = OutputBuffer = RegisterA + RegisterB;
                    break;

                case Operation.Subtract:
                    OutputBuffer = OutputBuffer = RegisterA - RegisterB;
                    break;

                case Operation.Divide:
                    if (RegisterB == 0)
                    {
                        BufferText.Text = "Divide by 0";
                        RegisterBSet = false;
                        SetOutputTextToOutputBuffer();
                        return;
                    }
                    OutputBuffer = OutputBuffer = RegisterA / RegisterB;
                    break;

                case Operation.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Safety check the length of the output
            if (OutputBuffer.ToString(CultureInfo.CurrentCulture).Length > 8)
            {
                Debug.WriteLine("Answer is too long.");
                BufferText.Text = "ERR";
                return;
            }

            Debug.WriteLine("Result = " + OutputBuffer);

            SetOutputTextToOutputBuffer();
            ClearInputBuffer();
            CurrentOperation = Operation.None;
            RegisterA = OutputBuffer;
            RegisterB = 0;
            RegisterBSet = false;
        }

        /// <summary>
        /// Tries to invert the current InputBuffer value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleNegative(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"{nameof(ToggleNegative)}({InputBuffer})");

            // We clear the result (if being shown) to show that the number
            // the user is trying to invert is actually nothing (gaslighting)
            if (IsShowingResult)
                ClearResult();

            // No point in inverting 0, save the parsing
            if (InputBuffer is "0" or "")
                return;

            InputBuffer = (float.Parse(InputBuffer) * -1).ToString(CultureInfo.CurrentCulture);
            SetOutputTextToInputBuffer();
        }

        /// <summary>
        /// Decimates the current InputBuffer value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Decimal(object sender, RoutedEventArgs e)
        {
            // We need a number to be able to decimate it
            if (InputBuffer.Length == 0)
            {
                Debug.WriteLine("There are no numbers in the input buffer to decimate.");
                return;
            }

            // We can't decimate a number multiple times (i think)
            if (InputBuffer.Contains('.'))
            {
                Debug.WriteLine("Can't decimate an already decimated number.");
                return;
            }

            InputBuffer += ".";
            SetOutputTextToInputBuffer();
        }

        /// <summary>
        /// Sets the operation mode to 'Divide'.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Divide(object sender, RoutedEventArgs e) => SetOperation(Operation.Divide);

        /// <summary>
        /// Sets the operation mode to 'Add'.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Plus(object sender, RoutedEventArgs e) => SetOperation(Operation.Add);

        /// <summary>
        /// Sets the operation mode to 'Subtract'.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minus(object sender, RoutedEventArgs e) => SetOperation(Operation.Subtract);

        /// <summary>
        /// Forces a calculation using the current regA and regB values with the set mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Equal(object sender, RoutedEventArgs e) => Calculate();

        #region Numbered Buttons

        // Bet you can't guess what these buttons do.

        private void Seven(object sender, RoutedEventArgs e) => DigitPressed(7);

        private void Eight(object sender, RoutedEventArgs e) => DigitPressed(8);

        private void Nine(object sender, RoutedEventArgs e) => DigitPressed(9);

        private void Four(object sender, RoutedEventArgs e) => DigitPressed(4);

        private void Five(object sender, RoutedEventArgs e) => DigitPressed(5);

        private void Six(object sender, RoutedEventArgs e) => DigitPressed(6);

        private void Three(object sender, RoutedEventArgs e) => DigitPressed(3);

        private void Two(object sender, RoutedEventArgs e) => DigitPressed(2);

        private void One(object sender, RoutedEventArgs e) => DigitPressed(1);

        private void Zero(object sender, RoutedEventArgs e) => DigitPressed(0);

        #endregion Numbered Buttons
    }
}