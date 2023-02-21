using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PowerDocu.Common
{
    public class Expression
    {
        public string expressionOperator;
        public List<object> expressionOperands = new List<object>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\"").Append(expressionOperator).Append("\": ");
            int counter = 0;
            if (expressionOperands.Count > 1)
            {
                sb.Append("{");
            }
            foreach (object expOperand in expressionOperands)
            {
                if (expOperand.GetType().Equals(typeof(List<object>)))
                {
                    sb.Append(createStringFromExpressionList((List<object>)expOperand));
                }
                else
                {
                    if (expOperand.GetType().Equals(typeof(Expression)))
                    {
                        sb.Append(expOperand.ToString());
                    }
                    else
                    {
                        bool isNumber = int.TryParse(expOperand.ToString(), out int i);
                        if (!isNumber)
                        {
                            sb.Append("\"").Append(expOperand.ToString()).Append("\"");
                        }
                        else
                        {
                            sb.Append(i);
                        }
                    }
                }
                if (expressionOperands.Count > 1 && ++counter != expressionOperands.Count)
                {
                    sb.Append(", ");
                }
            }
            if (expressionOperands.Count > 1)
            {
                sb.Append("}");
            }
            return sb.ToString();
        }

        public static Expression parseExpressions(JProperty jsonExpression)
        {
            Expression expression = new Expression
            {
                expressionOperator = jsonExpression.Name
            };
            if (jsonExpression.Value.GetType().Equals(typeof(Newtonsoft.Json.Linq.JArray)))
            {
                JArray operands = (JArray)jsonExpression.Value;
                List<object> operandsArray = new List<object>();
                foreach (JToken operandExpression in operands)
                {
                    if (operandExpression.GetType().Equals(typeof(Newtonsoft.Json.Linq.JValue)))
                    {
                        operandsArray.Add(operandExpression.ToString());
                    }
                    else if (operandExpression.GetType().Equals(typeof(Newtonsoft.Json.Linq.JObject)))
                    {
                        List<object> parsedOperands = new List<object>();
                        foreach (JProperty inputNode in (JEnumerable<JToken>)operandExpression.Children())
                        {
                            parsedOperands.Add(parseExpressions(inputNode));
                        }
                        operandsArray.Add(parsedOperands);
                    }
                }
                expression.expressionOperands.Add(operandsArray);
            }
            else if (jsonExpression.Value.GetType().Equals(typeof(Newtonsoft.Json.Linq.JObject)))
            {
                JObject expressionObject = (JObject)jsonExpression.Value;
                foreach (JProperty inputNode in (JEnumerable<JToken>)expressionObject.Children())
                {
                    expression.expressionOperands.Add(parseExpressions(inputNode));
                }
            }
            else if (jsonExpression.Value.GetType().Equals(typeof(Newtonsoft.Json.Linq.JValue)))
            {
                expression.expressionOperands.Add(jsonExpression.Value.ToString());
            }
            return expression;
        }

        public static string createStringFromExpressionList(List<object> operands)
        {
            StringBuilder sb = new StringBuilder("[");
            int counter = 0;
            foreach (object operand in operands)
            {
                if (operand.GetType().Equals(typeof(List<object>)))
                {
                    List<object> operandlist = (List<object>)operand;
                    sb.Append("{");
                    int innerOperandCounter = 0;
                    foreach (object innerOperand in operandlist)
                    {
                        if (innerOperand.GetType().Equals(typeof(Expression)))
                        {
                            sb.Append(((Expression)innerOperand).ToString());
                        }
                        else
                        {
                            //todo check
                            string a = "";
                        }
                        if (operandlist.Count > 1 && ++innerOperandCounter != operandlist.Count)
                        {
                            sb.Append(", ");
                        }
                    }
                    sb.Append("}");
                }
                else if (operand.GetType().Equals(typeof(string)))
                {
                    bool isNumber = int.TryParse((string)operand, out int i);
                    if (!isNumber)
                    {
                        sb.Append("\"").Append((string)operand).Append("\"");
                    }
                    else
                    {
                        sb.Append(i);
                    }
                }
                else
                {
                    string t = "";
                }
                if (operands.Count > 1 && ++counter != operands.Count)
                {
                    sb.Append(", ");
                }
            }
            sb.Append("]");
            return JsonUtil.JsonPrettify(sb.ToString());
        }
    }
}