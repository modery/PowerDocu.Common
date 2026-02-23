using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace PowerDocu.Common
{
    /// <summary>
    /// Base class for HTML documentation builders. Provides helper methods
    /// for generating HTML content with a consistent template approach.
    /// The visual design is driven by an external CSS stylesheet, making
    /// it easy to customise the look-and-feel without touching the code.
    /// </summary>
    public abstract class HtmlBuilder
    {
        protected readonly Random random = new Random();

        // ------------------------------------------------------------------
        // Template helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the full HTML page wrapping the given body content.
        /// A &lt;link&gt; to <c>style.css</c> is included so that users can
        /// swap the stylesheet to change the design.
        /// </summary>
        protected string WrapInHtmlPage(string title, string bodyContent, string navigationHtml, string cssRelativePath = "style.css")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"  <title>{Encode(title)}</title>");
            sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{cssRelativePath}\">");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"page-wrapper\">");
            sb.AppendLine("  <nav class=\"sidebar\">");
            sb.AppendLine(navigationHtml);
            sb.AppendLine("  </nav>");
            sb.AppendLine("  <main class=\"content\">");
            sb.AppendLine(bodyContent);
            sb.AppendLine("  </main>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        // Element helpers
        // ------------------------------------------------------------------

        protected static string Encode(string text)
        {
            return HttpUtility.HtmlEncode(text ?? "");
        }

        protected static string Heading(int level, string text)
        {
            return $"<h{level}>{Encode(text)}</h{level}>";
        }

        protected static string HeadingWithId(int level, string text, string id)
        {
            return $"<h{level} id=\"{Encode(id)}\">{Encode(text)}</h{level}>";
        }

        protected static string HeadingRaw(int level, string innerHtml)
        {
            return $"<h{level}>{innerHtml}</h{level}>";
        }

        protected static string Paragraph(string text)
        {
            return $"<p>{Encode(text)}</p>";
        }

        protected static string ParagraphRaw(string innerHtml)
        {
            return $"<p>{innerHtml}</p>";
        }

        protected static string Link(string text, string href)
        {
            return $"<a href=\"{Encode(href)}\">{Encode(text)}</a>";
        }

        /// <summary>
        /// Creates a URL-safe anchor ID from a control/element name.
        /// Lowercases, replaces spaces with hyphens, and removes unsafe characters.
        /// </summary>
        protected static string SanitizeAnchorId(string name)
        {
            if (String.IsNullOrEmpty(name)) return "";
            return System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant().Replace(" ", "-"), "[^a-z0-9_-]", "");
        }

        protected static string Image(string alt, string src)
        {
            return $"<img src=\"{Encode(src)}\" alt=\"{Encode(alt)}\" />";
        }

        protected static string ImageWithClass(string alt, string src, string cssClass)
        {
            return $"<img src=\"{Encode(src)}\" alt=\"{Encode(alt)}\" class=\"{cssClass}\" />";
        }

        protected static string CodeBlock(string code)
        {
            if (String.IsNullOrEmpty(code)) return "";
            return $"<code>{Encode(code)}</code>";
        }

        // ------------------------------------------------------------------
        // Table helpers
        // ------------------------------------------------------------------

        protected static string TableStart(params string[] headers)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr>");
            foreach (string h in headers)
            {
                sb.AppendLine($"  <th>{Encode(h)}</th>");
            }
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("<tbody>");
            return sb.ToString();
        }

        protected static string TableRow(params string[] cells)
        {
            StringBuilder sb = new StringBuilder("<tr>");
            foreach (string c in cells)
            {
                sb.Append($"<td>{Encode(c)}</td>");
            }
            sb.Append("</tr>");
            return sb.ToString();
        }

        /// <summary>
        /// Table row allowing raw HTML in cells (caller is responsible for encoding).
        /// </summary>
        protected static string TableRowRaw(params string[] cells)
        {
            StringBuilder sb = new StringBuilder("<tr>");
            foreach (string c in cells)
            {
                sb.Append($"<td>{c}</td>");
            }
            sb.Append("</tr>");
            return sb.ToString();
        }

        protected static string TableEnd()
        {
            return "</tbody></table>";
        }

        // ------------------------------------------------------------------
        // Navigation helpers
        // ------------------------------------------------------------------

        protected static string NavigationList(IEnumerable<(string label, string href)> items)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<ul class=\"nav-list\">");
            foreach (var item in items)
            {
                sb.AppendLine($"  <li><a href=\"{Encode(item.href)}\">{Encode(item.label)}</a></li>");
            }
            sb.AppendLine("</ul>");
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        // Bullet list helpers
        // ------------------------------------------------------------------

        protected static string BulletListStart() => "<ul>";
        protected static string BulletListEnd() => "</ul>";
        protected static string BulletItem(string text) => $"<li>{Encode(text)}</li>";
        protected static string BulletItemRaw(string innerHtml) => $"<li>{innerHtml}</li>";

        // ------------------------------------------------------------------
        // Expression helpers (mirrors MarkdownBuilder helpers)
        // ------------------------------------------------------------------

        protected string AddExpressionDetails(List<Expression> inputs)
        {
            StringBuilder tableSB = new StringBuilder("<table class=\"expression-table\">");
            foreach (Expression input in inputs)
            {
                StringBuilder operandsCellSB = new StringBuilder("<td>");

                if (input.expressionOperands.Count > 1)
                {
                    StringBuilder operandsTableSB = new StringBuilder("<table class=\"expression-table\">");
                    foreach (object actionInputOperand in input.expressionOperands)
                    {
                        if (actionInputOperand.GetType() == typeof(Expression))
                        {
                            operandsTableSB.Append(AddExpressionTable((Expression)actionInputOperand, false));
                        }
                        else
                        {
                            operandsTableSB.Append("<tr><td>").Append(Encode(actionInputOperand.ToString())).Append("</td></tr>");
                        }
                    }
                    operandsTableSB.Append("</table>");
                    operandsCellSB.Append(operandsTableSB).Append("</td>");
                }
                else
                {
                    if (input.expressionOperands.Count > 0)
                    {
                        if (input.expressionOperands[0]?.GetType() == typeof(Expression))
                        {
                            operandsCellSB.Append(AddExpressionTable((Expression)input.expressionOperands[0]).Append("</table>"));
                        }
                        else if (input.expressionOperands[0]?.GetType() == typeof(string))
                        {
                            operandsCellSB.Append(Encode(input.expressionOperands[0]?.ToString()));
                        }
                        else if (input.expressionOperands[0]?.GetType() == typeof(List<object>))
                        {
                            operandsCellSB.Append("<table class=\"expression-table\">");
                            foreach (object obj in (List<object>)input.expressionOperands[0])
                            {
                                if (obj.GetType().Equals(typeof(Expression)))
                                {
                                    operandsCellSB.Append(AddExpressionTable((Expression)obj, false));
                                }
                                else if (obj.GetType().Equals(typeof(List<object>)))
                                {
                                    foreach (object o in (List<object>)obj)
                                    {
                                        operandsCellSB.Append(AddExpressionTable((Expression)o, false));
                                    }
                                }
                            }
                            operandsCellSB.Append("</table>");
                        }
                    }
                    else
                    {
                        operandsCellSB.Append("");
                    }
                    operandsCellSB.Append("</td>");
                }
                tableSB.Append("<tr><td>").Append(Encode(input.expressionOperator)).Append("</td>").Append(operandsCellSB).Append("</tr>");
            }
            tableSB.Append("</table>");
            return tableSB.ToString();
        }

        protected StringBuilder AddExpressionTable(Expression expression, bool createNewTable = true, bool firstColumnBold = false)
        {
            StringBuilder table = createNewTable ? new StringBuilder("<table class=\"expression-table\">") : new StringBuilder();

            if (expression?.expressionOperator != null)
            {
                StringBuilder tr = new StringBuilder("<tr>");
                StringBuilder tc = new StringBuilder("<td>");

                if (firstColumnBold)
                {
                    tc.Append("<b>").Append(Encode(expression.expressionOperator)).Append("</b>");
                }
                else
                {
                    tc.Append(Encode(expression.expressionOperator));
                }
                tr.Append(tc.Append("</td>"));
                tc = new StringBuilder("<td>");
                if (expression.expressionOperands.Count > 1)
                {
                    StringBuilder operandsTable = new StringBuilder("<table class=\"expression-table\">");
                    foreach (var expressionOperand in expression.expressionOperands.OrderBy(o => o.ToString()).ToList())
                    {
                        if (expressionOperand.GetType().Equals(typeof(string)))
                        {
                            operandsTable.Append("<tr><td>").Append(CodeBlock((string)expressionOperand)).Append("</td></tr>");
                        }
                        else if (expressionOperand.GetType().Equals(typeof(Expression)))
                        {
                            operandsTable.Append(AddExpressionTable((Expression)expressionOperand, false));
                        }
                        else
                        {
                            operandsTable.Append("<tr><td></td></tr>");
                        }
                    }
                    tc.Append(operandsTable).Append("</table>");
                }
                else if (expression.expressionOperands.Count == 0)
                {
                    // nothing to do
                }
                else
                {
                    object expo = expression.expressionOperands[0];
                    if (expo.GetType().Equals(typeof(string)))
                    {
                        tc.Append((expression.expressionOperands.Count == 0) ? "" : CodeBlock(expression.expressionOperands[0]?.ToString()));
                    }
                    else if (expo.GetType().Equals(typeof(List<object>)))
                    {
                        foreach (object obj in (List<object>)expo)
                        {
                            if (obj.GetType().Equals(typeof(List<object>)))
                            {
                                foreach (object o in (List<object>)obj)
                                {
                                    tc.Append(AddExpressionTable((Expression)o, true));
                                }
                            }
                            else if (obj.GetType().Equals(typeof(Expression)))
                            {
                                tc.Append(AddExpressionTable((Expression)obj, true));
                            }
                            else
                            {
                                tc.Append(Encode(obj.ToString())).Append("<br/>");
                            }
                        }
                    }
                    else if (expo.GetType().Equals(typeof(Expression)))
                    {
                        tc.Append(AddExpressionTable((Expression)expo, true));
                    }
                }
                tr.Append(tc).Append("</td>");
                table.Append(tr.Append("</tr>"));
            }
            if (createNewTable)
            {
                table.Append("</table>");
            }
            return table;
        }

        // ------------------------------------------------------------------
        // File helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Writes the default CSS stylesheet to the target folder if it does
        /// not already exist. This allows it to be replaced with a custom one.
        /// </summary>
        protected static void WriteDefaultStylesheet(string folderPath)
        {
            string cssPath = Path.Combine(folderPath, "style.css");
            if (!File.Exists(cssPath))
            {
                File.WriteAllText(cssPath, GetDefaultCss());
            }
        }

        protected void SaveHtmlFile(string filePath, string htmlContent)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, htmlContent, Encoding.UTF8);
        }

        // ------------------------------------------------------------------
        // Default CSS
        // ------------------------------------------------------------------

        public static string GetDefaultCss()
        {
            return @"/* PowerDocu HTML Documentation Stylesheet
   Replace this file to change the visual appearance of the generated documentation. */

:root {
    --color-primary: #0078d4;
    --color-primary-dark: #005a9e;
    --color-bg: #ffffff;
    --color-bg-alt: #f5f6fa;
    --color-sidebar: #1e1e2e;
    --color-sidebar-text: #cdd6f4;
    --color-sidebar-hover: #313244;
    --color-text: #1e1e2e;
    --color-text-light: #585b70;
    --color-border: #e0e0e0;
    --color-success: #ccffcc;
    --color-danger: #ffcccc;
    --radius: 8px;
    --shadow: 0 1px 3px rgba(0,0,0,0.08);
    --font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
    --font-mono: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
}

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

body {
    font-family: var(--font-family);
    font-size: 15px;
    line-height: 1.6;
    color: var(--color-text);
    background: var(--color-bg);
}

/* Page layout */
.page-wrapper {
    display: flex;
    min-height: 100vh;
}

/* Sidebar navigation */
.sidebar {
    width: 260px;
    min-width: 260px;
    background: var(--color-sidebar);
    color: var(--color-sidebar-text);
    padding: 1.5rem 0;
    position: sticky;
    top: 0;
    height: 100vh;
    overflow-y: auto;
}

.sidebar .nav-title {
    font-size: 1.1rem;
    font-weight: 600;
    padding: 0 1.25rem 1rem;
    color: #fff;
    border-bottom: 1px solid var(--color-sidebar-hover);
    margin-bottom: 0.5rem;
}

.nav-list {
    list-style: none;
    padding: 0;
}

.nav-list li a {
    display: block;
    padding: 0.5rem 1.25rem;
    color: var(--color-sidebar-text);
    text-decoration: none;
    font-size: 0.9rem;
    transition: background 0.15s, color 0.15s;
    border-left: 3px solid transparent;
}

.nav-list li a:hover,
.nav-list li a.active {
    background: var(--color-sidebar-hover);
    color: #fff;
    border-left-color: var(--color-primary);
}

/* Main content area */
.content {
    flex: 1;
    padding: 2rem 3rem;
    max-width: 1100px;
}

h1 {
    font-size: 1.75rem;
    font-weight: 700;
    color: var(--color-primary-dark);
    margin-bottom: 1rem;
    padding-bottom: 0.5rem;
    border-bottom: 2px solid var(--color-primary);
}

h2 {
    font-size: 1.35rem;
    font-weight: 600;
    color: var(--color-text);
    margin: 1.75rem 0 0.75rem;
    padding-bottom: 0.35rem;
    border-bottom: 1px solid var(--color-border);
}

h3 {
    font-size: 1.1rem;
    font-weight: 600;
    margin: 1.25rem 0 0.5rem;
}

h4 {
    font-size: 1rem;
    font-weight: 600;
    margin: 1rem 0 0.5rem;
}

p { margin-bottom: 0.75rem; }

a { color: var(--color-primary); text-decoration: none; }
a:hover { text-decoration: underline; }

/* Tables */
table {
    width: 100%;
    border-collapse: collapse;
    margin: 0.75rem 0 1.25rem;
    background: var(--color-bg);
    border-radius: var(--radius);
    overflow: hidden;
    box-shadow: var(--shadow);
}

thead {
    background: var(--color-primary);
    color: #fff;
}

th {
    padding: 0.6rem 0.85rem;
    text-align: left;
    font-weight: 600;
    font-size: 0.85rem;
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

td {
    padding: 0.55rem 0.85rem;
    border-bottom: 1px solid var(--color-border);
    vertical-align: top;
    font-size: 0.9rem;
}

tbody tr:nth-child(even) { background: var(--color-bg-alt); }
tbody tr:hover { background: #eef2ff; }

/* Nested expression tables */
table.expression-table {
    box-shadow: none;
    margin: 0.25rem 0;
    font-size: 0.85rem;
}

table.expression-table thead { background: var(--color-text-light); }

/* Code blocks */
code {
    font-family: var(--font-mono);
    font-size: 0.85em;
    background: var(--color-bg-alt);
    padding: 0.15em 0.4em;
    border-radius: 4px;
    border: 1px solid var(--color-border);
}

/* Images */
img {
    max-width: 100%;
    height: auto;
    border-radius: var(--radius);
}

img.icon-inline {
    width: 16px;
    height: 16px;
    vertical-align: middle;
    border-radius: 0;
}

/* Lists */
ul, ol {
    padding-left: 1.5rem;
    margin-bottom: 0.75rem;
}

li { margin-bottom: 0.25rem; }

/* Color preview swatch */
.color-swatch {
    display: inline-block;
    width: 20px;
    height: 20px;
    border: 1px solid var(--color-border);
    border-radius: 4px;
    vertical-align: middle;
    margin-right: 0.35rem;
}

/* Changed defaults highlight */
.changed-value { background-color: var(--color-success); padding: 0.2rem 0.4rem; border-radius: 4px; }
.default-value { background-color: var(--color-danger); padding: 0.2rem 0.4rem; border-radius: 4px; }

/* Responsive */
@media (max-width: 768px) {
    .page-wrapper { flex-direction: column; }
    .sidebar {
        width: 100%;
        min-width: 100%;
        height: auto;
        position: relative;
    }
    .content { padding: 1rem; }
}
";
        }
    }
}
