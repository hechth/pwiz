using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ProteinTurnoverArgCollector.Properties;
using Microsoft.VisualBasic.FileIO;

namespace ProteinTurnoverArgCollector
{


    public enum DefinedValues
    {
        All,
        Some,
        None
    }


    // ReSharper disable once InconsistentNaming
    public partial class ProteinTurnoverUI : Form
    {
        public string[] Arguments { get; private set; }
        private string dietEnrichmentDefault = "99";
        private string avgTurnoverDefault = "0";
        private string IDPDefault = "0";
        private string folderNameDefault = "Data";
        private bool qValueFilterDefault = true;
        private string qValueDevault = "1";
        
        private string dietEnrichmentRange = "(0,100]";
        private string avgTurnoverRange = "[0,1)";
        private string IDPRange = "[0,1)";
        private string qValueRange = "(0,1]";

        public static DefinedValues DEFINED_Q_VALUES;
        public static List<string> CONDITIONS;

        public ProteinTurnoverUI(string[] oldArguments)
        {
            InitializeComponent();
            Arguments = oldArguments;
            

            if (DEFINED_Q_VALUES == DefinedValues.None)
            {
                labelQValue.Text = "";
                labelQValue.Enabled = false;
                checkBoxFilterQValues.Checked = false;
                checkBoxFilterQValues.Enabled = false;
            }
            if (DEFINED_Q_VALUES == DefinedValues.Some)
            {
                labelQValueWarning.Visible = true;
            }

            foreach (var condition in CONDITIONS)
                comboReference.Items.Add(condition);


        }

        /// <summary>
        /// Display previous/default arguments in form when loaded. Add tooltips
        /// </summary>
        private void ProteinTurnoverUI_Load(object sender, EventArgs e)
        {
            if (Arguments != null)
            {
                textDietEnrichment.Text = Arguments[(int)ArgumentIndices.diet_enrichment];
                textAverageTurnover.Text = Arguments[(int)ArgumentIndices.average_turnover];
                textIDP.Text = Arguments[(int) ArgumentIndices.IDP];
                textFolderName.Text = Arguments[(int) ArgumentIndices.folder_name];
                checkBoxFilterQValues.Checked = checkBoxFilterQValues.Enabled && 
                                                Arguments[(int) ArgumentIndices.filter_Q_values].Equals(Constants.TRUE_STRING);
                textQValue.Text = Arguments[(int) ArgumentIndices.Q_value];
            }
            else
            {
                textDietEnrichment.Text = dietEnrichmentDefault;
                textAverageTurnover.Text = avgTurnoverDefault;
                textIDP.Text = IDPDefault;
                textFolderName.Text = folderNameDefault;
                checkBoxFilterQValues.Checked = checkBoxFilterQValues.Enabled && qValueFilterDefault;
                textQValue.Text = qValueDevault;
            }
        }

        /// <summary>
        /// "Ok" button click event.  If VerifyArgument() returns true will generate arguments.
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (VerifyArguments())
            {
                GenerateArguments();
                DialogResult = DialogResult.OK;
            }
        }

        /// <summary>
        /// "Cancel" button click event.  Closes form without generating arguments.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

        }

        /// <summary>
        /// Run before arguments are generated and can return an error message to
        /// the user.  If it returns true arguments will be generated.
        /// </summary>
        private bool VerifyArguments()
        {
            // Checks number text boxes
            List<TextBox> numberInputBoxes = new List<TextBox>{
                textDietEnrichment,
                textAverageTurnover,
                textIDP
            };
            List<string> inputRanges = new List<string> {
                dietEnrichmentRange,
                avgTurnoverRange,
                IDPRange
            };

            if (checkBoxFilterQValues.Checked)
            {
                numberInputBoxes.Add(textQValue);
                inputRanges.Add(qValueRange);
            }

            for (int i = 0; i < numberInputBoxes.Count; i++)
            {
                if (!VerifyInputNumber(numberInputBoxes[i], inputRanges[i]))
                {
                    return false;
                }
            }

            if (textFolderName.Text == "")
            {
                MessageBox.Show(Resources.ProteinTurnoverUI_VerifyArguments_Folder_name_cannot_be_blank);
                return false;
            }
            

            if (comboReference.SelectedIndex < 0)
            {
                MessageBox.Show(Resources.ProteinTurnoverUI_VerifyArguments_Please_select_a_treatment_group_);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Checks that a texbox is a valid double within input range
        /// </summary>
        private bool VerifyInputNumber(TextBox inputBox, string inputRange)
        {
            double number;
            try
            {
                number = double.Parse(inputBox.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show(Resources.ProteinTurnoverUI_VerifyArguments_Input_must_be_number);
                ActiveControl = inputBox;
                return false;
            }

            bool minInclusive = inputRange[0] == '[';
            bool maxInclusive = inputRange[inputRange.Length - 1] == ']';
            string[] range = inputRange.Substring(1, inputRange.Length - 2).Split(',');
            string min = range[0];
            string max = range[1];
            bool inRange = true;
            double TOLERANCE = 0.00000000001;
            if (Double.TryParse(min, out double minDouble))
            {
                if (number < minDouble || (Math.Abs(number - minDouble) < TOLERANCE && !minInclusive))
                {
                    inRange = false;
                }
            }

            if (Double.TryParse(max, out double maxDouble))
            {
                if (number > maxDouble || (Math.Abs(number - maxDouble) < TOLERANCE && !maxInclusive))
                {
                    inRange = false;
                }
            }

            if (!inRange)
            {
                string textBounds = String.Format(Resources.ProteinTurnoverUI_VerifyInputNumber_The_number__0__must_be_between__1__and__2_, inputBox.Text, min, max);
                textBounds = min == "" ? String.Format(Resources.ProteinTurnoverUI_VerifyInputNumber_The_number__0__must_be_less_than__1_, inputBox.Text, max) : textBounds;
                textBounds = max == "" ? String.Format(Resources.ProteinTurnoverUI_VerifyInputNumber_The_number__0__must_be_greater_than__1_, inputBox.Text, min) : textBounds;

                MessageBox.Show(textBounds);
            }

            return inRange;
        }

        /// <summary>
        /// Generates an Arguments[] for the values of the user inputs.
        /// Number of Arguments is defined in TestToolUtil.cs
        /// </summary>
        public void GenerateArguments()
        {
            Arguments = new string[Constants.ARGUMENT_COUNT];
            Arguments[(int) ArgumentIndices.diet_enrichment] = textDietEnrichment.Text;
            Arguments[(int) ArgumentIndices.average_turnover] = textAverageTurnover.Text;
            Arguments[(int) ArgumentIndices.IDP] = textIDP.Text;
            Arguments[(int) ArgumentIndices.folder_name] = textFolderName.Text;
            Arguments[(int) ArgumentIndices.reference_group] = comboReference.GetItemText(comboReference.SelectedItem);
            Arguments[(int) ArgumentIndices.Q_value] = textQValue.Enabled ? textQValue.Text : "1";
            Arguments[(int) ArgumentIndices.filter_Q_values] = checkBoxFilterQValues.Checked
                ? Constants.TRUE_STRING
                : Constants.FALSE_STRING;
        }

        private void checkBoxFilterQValues_CheckedChanged(object sender, EventArgs e)
        {
            textQValue.Enabled = checkBoxFilterQValues.Checked;
            labelQValue.Enabled = checkBoxFilterQValues.Checked;
            labelQValueWarning.Visible = checkBoxFilterQValues.Checked && DEFINED_Q_VALUES == DefinedValues.Some;
        }
    }

    public class ArgCollector
    {
        public static string[] CollectArgs(IWin32Window parent, TextReader report, string[] oldArgs)
        {
            var detectionQValue = "DetectionQValue";
            var condition = "Condition";
            var columns = GetCsvColumns(report, new[] { detectionQValue, condition });

            var qValueString = new StringBuilder();
            foreach (var qValue in columns[detectionQValue])
            {
                qValueString.Append(qValue).AppendLine();
            }


            
            var defined = 0;

            foreach (var qValue in columns[detectionQValue])
            {
                if (Double.TryParse(qValue, out _))
                {
                    defined++;
                }
            }

            if (defined == columns[detectionQValue].Count) ProteinTurnoverUI.DEFINED_Q_VALUES = DefinedValues.All;
            if (defined < columns[detectionQValue].Count && defined > 0) ProteinTurnoverUI.DEFINED_Q_VALUES = DefinedValues.Some;
            if (defined == 0) ProteinTurnoverUI.DEFINED_Q_VALUES = DefinedValues.None;

            

            var conditionString = new StringBuilder();
            foreach (var c in columns[condition])
            {
                conditionString.Append(c).AppendLine();
            }

            ProteinTurnoverUI.CONDITIONS = columns[condition].ToList();
            

            using (var dlg = new ProteinTurnoverUI(oldArgs))
            {
                return (dlg.ShowDialog(parent) == DialogResult.OK) ? dlg.Arguments : null;
            }

            
        }


        public static Dictionary<string, ICollection<string>> GetCsvColumns(TextReader report, string[] columnNames)
        {
            //const string qValueColumnName = "DetectionQValue"; // Not L10N
            //const string invalidValue = "#N/A";
            var parser = new TextFieldParser(report);
            parser.SetDelimiters(",");
            string[] fields = parser.ReadFields() ?? new string[0];
            var columnIndicies = new Dictionary<string, int>();
            var columns = new Dictionary<string, ICollection<string>>();
            foreach (var columnName in columnNames)
            {
                int groupIndex = Array.IndexOf(fields, columnName);
                if (groupIndex >= 0)
                {
                    columnIndicies.Add(columnName, groupIndex);
                    columns.Add(columnName, new HashSet<string>());
                }
            }
            
            // The last line in the CSV file is empty, thus we compare length - 1 
            string[] line;
            while ((line = parser.ReadFields()) != null)
            {
                foreach (var columnName in columns.Keys)
                {
                    columns[columnName].Add(line[columnIndicies[columnName]]);
                }
            }

            return columns;
        }

    }
}
