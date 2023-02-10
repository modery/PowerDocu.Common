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
            foreach (object eo in expressionOperands)
            {
                if (eo.GetType().Equals(typeof(List<object>)))
                {
                    sb.Append(createStringFromExpressionList((List<object>)eo));
                }
                else
                {
                    if (eo.GetType().Equals(typeof(Expression)))
                    {
                        sb.Append(eo.ToString());
                    }
                    else
                    {
                        bool isNumber = int.TryParse(eo.ToString(), out int i);
                        if (!isNumber)
                        {
                            sb.Append("\"").Append(eo.ToString()).Append("\"");
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
                        //expression.expressionOperands.Add(operandExpression.ToString());
                        operandsArray.Add(operandExpression.ToString());
                    }
                    else if (operandExpression.GetType().Equals(typeof(Newtonsoft.Json.Linq.JObject)))
                    {
                        //todo this here causes an issue when there are multiple children or so
                        List<object> parsedOperands = new List<object>();
                        foreach (JProperty inputNode in (JEnumerable<JToken>)operandExpression.Children())
                        {
                            //expression.expressionOperands.Add(parseExpressions(inputNode));
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

        public static string createStringFromExpressionList(List<object> ops)
        {
            StringBuilder s = new StringBuilder("[");
            int counter = 0;
            foreach (object o in ops)
            {
                if (o.GetType().Equals(typeof(List<object>)))
                {
                    List<object> so = (List<object>)o;
                    s.Append("{");
                    int soocounter = 0;
                    foreach (object soo in so)
                    {
                        if (soo.GetType().Equals(typeof(Expression)))
                        {
                            s.Append(((Expression)soo).ToString());
                        }
                        else
                        {
                            //todo check
                            string a = "";
                        }
                        if (so.Count > 1 && ++soocounter != so.Count)
                        {
                            s.Append(", ");
                        }
                    }
                    s.Append("}");
                    if (ops.Count > 1 && counter != ops.Count)
                    {
                        counter++;
                        s.Append(",");
                    }
                }
                else if (o.GetType().Equals(typeof(string)))
                {
                    s.Append((string)o);
                }
                else
                {
                    string t = "";
                }
                if (ops.Count > 1 && ++counter != ops.Count)
                {
                    s.Append(", ");
                }
            }
            s.Append("]");
            return JsonUtil.JsonPrettify(s.ToString());
        }
    }
}