using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Web;

namespace MiResiliencia.Helpers.API
{
    public static class HtmlTools
    {
        public static string ObjectToHtmlTable<T>(this T item, string tableSyle, string headerStyle, string rowStyle, string alternateRowStyle, bool showDetails = false)
        {
            if (item == null)
                return string.Empty;

            IEnumerable<T> list = new List<T> { item };
            return list.ToHtmlTable(tableSyle, headerStyle, rowStyle, alternateRowStyle, showDetails);
        }

        public static string ToHtmlTable<T>(this IEnumerable<T> list, string tableSyle, string headerStyle, string rowStyle, string alternateRowStyle, bool showDetails = false)
        {
            if (list == null)
                return string.Empty;

            var result = new StringBuilder();

            if (string.IsNullOrEmpty(tableSyle))
            {
                result.Append("<table class=\"table\" id=\"" + typeof(T).Name + "Table\">");
            }
            else
            {
                result.Append("<table id=\"" + typeof(T).Name + "Table\" class=\"" + tableSyle + "\">");
            }

            var propertyArray = typeof(T).GetProperties();

            result.Append("<thead>");
            foreach (System.Reflection.PropertyInfo prop in propertyArray)
            {
                // Show only if there is no "TableIgnore" Annotation for property
                if (prop.GetCustomAttributes(typeof(TableIgnoreAttribute), true).Count() == 0)
                {
                    if (prop.GetCustomAttributes(typeof(ShowInDetailAttribute), true).Any() && showDetails || !prop.GetCustomAttributes(typeof(ShowInDetailAttribute), true).Any())
                    {

                        if (string.IsNullOrEmpty(headerStyle))
                        {
                            if (prop.GetCustomAttributes(typeof(DisplayNameAttribute), true).Count() > 0)
                            {
                                result.AppendFormat("<th>{0}</th>", ((DisplayNameAttribute)prop.GetCustomAttributes(typeof(DisplayNameAttribute), true)[0]).DisplayName);
                            }
                            else
                            {
                                result.AppendFormat("<th>{0}</th>", prop.Name);
                            }
                        }
                        else
                        {
                            if (prop.GetCustomAttributes(typeof(DisplayNameAttribute), true).Count() > 0)
                            {
                                result.AppendFormat("<th class=\"{0}\">{1}</th>", headerStyle, ((DisplayNameAttribute)prop.GetCustomAttributes(typeof(DisplayNameAttribute), true)[0]).DisplayName);
                            }
                            else
                            {
                                result.AppendFormat("<th class=\"{0}\">{1}</th>", headerStyle, prop.Name);
                            }
                        }
                    }



                }
            }
            result.Append("</thead>");

            for (int i = 0; i < list.Count(); i++)
            {
                if (list.ElementAt(i) == null)
                    continue;

                if (!string.IsNullOrEmpty(rowStyle) && !string.IsNullOrEmpty(alternateRowStyle))
                {
                    result.AppendFormat("<tr class=\"{0}\">", i % 2 == 0 ? rowStyle : alternateRowStyle);
                }
                else
                {
                    result.AppendFormat("<tr>");
                }

                foreach (var prop in propertyArray)
                {
                    if (prop.GetCustomAttributes(typeof(TableIgnoreAttribute), true).Count() == 0)
                    {
                        if (prop.GetCustomAttributes(typeof(ShowInDetailAttribute), true).Any() && showDetails || !prop.GetCustomAttributes(typeof(ShowInDetailAttribute), true).Any())
                        {


                            object value = prop.GetValue(list.ElementAt(i), null);

                            if (prop.GetCustomAttributes(typeof(DisplayFormatAttribute), true).Any())
                            {
                                DisplayFormatAttribute displayFormat = (DisplayFormatAttribute)prop.GetCustomAttributes(typeof(DisplayFormatAttribute), true).First();

                                if (displayFormat.DataFormatString == null)
                                {
                                    // try LocalizedDisplayFormatAttribute
                                    LocalizedDisplayFormatAttribute locDisplayFormat = (LocalizedDisplayFormatAttribute)prop.GetCustomAttributes(typeof(LocalizedDisplayFormatAttribute), true).First();
                                    displayFormat.DataFormatString = locDisplayFormat.DataFormatString;
                                }

                                if (value.GetType() == typeof(Tuple<double, string>))
                                {
                                    result.AppendFormat("<td>{0}</td>", string.Format(displayFormat.DataFormatString, ((Tuple<double, string>)value).Item1));
                                }
                                else
                                {
                                    result.AppendFormat("<td>{0}</td>", string.Format(displayFormat.DataFormatString, value));
                                }
                            }
                            else
                            {
                                if (value == null)
                                {
                                    result.AppendFormat("<td>{0}</td>", string.Empty);
                                }
                                else if (value.GetType() == typeof(double))
                                {
                                    result.AppendFormat("<td>{0}</td>", ((double)value).ToString("#,##0.000") ?? string.Empty);
                                }
                                else if (value.GetType() == typeof(decimal))
                                {
                                    result.AppendFormat("<td>{0}</td>", ((decimal)value).ToString("#,##0.000") ?? string.Empty);
                                }
                                else if (value.GetType() == typeof(int))
                                {
                                    result.AppendFormat("<td>{0}</td>", ((int)value).ToString("#,##0") ?? string.Empty);
                                }
                                else result.AppendFormat("<td>{0}</td>", value ?? string.Empty);

                            }
                        }
                    }
                }
                result.AppendLine("</tr>");
            }

            result.Append("</table>");

            return result.ToString();
        }
    }
}