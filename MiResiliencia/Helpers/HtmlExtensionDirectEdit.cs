using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MiResiliencia.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;

namespace MiResiliencia.Helpers
{
    
    public static class HtmlExtensionDirectEdit
    {
        public static string GetString(this IHtmlContent content)
        {
            var writer = new System.IO.StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }

        public static IHtmlContent MyEditorFor<TModel, TProperty>(
    this IHtmlHelper<TModel> htmlHelper,
    Expression<Func<TModel, TProperty>> expression, string id)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new InvalidOperationException("Expression must be a member expression");

            string name = memberExpression.Member.Name;

            TagBuilder div = new TagBuilder("div");
            div.Attributes["id"] = id.ToString() + "_" + name + "_edit";
            div.Attributes["style"] = "display:none;";
            div.Attributes["class"] = "form-group insideEditor";
            TagBuilder divsub = new TagBuilder("div");
            divsub.Attributes["class"] = "col-md-12";
            IHtmlContent editor = HtmlHelperEditorExtensions.EditorFor(htmlHelper, expression);
            string e = editor.ToString();

            divsub.InnerHtml.AppendHtml(e.Replace("class=\"text-box single-line\"", "class=\"text-box single-line form-control\"").Replace("id=\"", "id=\"" + id + "_"));
            div.InnerHtml.AppendHtml(divsub.ToString());

            TagBuilder div2 = new TagBuilder("div");
            div2.Attributes["id"] = id.ToString() + "_" + name + "_text";
            div2.Attributes["class"] = "insideText";
            IHtmlContent display = HtmlHelperDisplayExtensions.DisplayFor(htmlHelper, expression);
            div2.InnerHtml.AppendHtml(display.ToString().Replace("id=\"a_", "id=\"" + id + "_showtext_"));

            return new HtmlString(div.ToString() + div2.ToString());


        }

        public static IHtmlContent MyEditorRowFor<TModel, TProperty>(
    this IHtmlHelper<TModel> htmlHelper,
    Expression<Func<TModel, TProperty>> expression, string id)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new InvalidOperationException("Expression must be a member expression");

            string name = memberExpression.Member.Name;

            DisplayNameAttribute? displayNameAttr =
                (DisplayNameAttribute?)Attribute.GetCustomAttribute(
                    htmlHelper.GetType(), typeof(DisplayNameAttribute));
            string displayName = displayNameAttr == null ?
                  htmlHelper.DisplayNameFor(expression) :
                  displayNameAttr.DisplayName.ToString();

            IHtmlContent display = HtmlHelperDisplayExtensions.DisplayFor(htmlHelper, expression);

            TagBuilder row = new TagBuilder("tr");
            TagBuilder desc = new TagBuilder("td");
            desc.Attributes["style"] = "font-weight:bold;";
            desc.InnerHtml.AppendHtml(displayName);
            row.InnerHtml.AppendHtml(desc);
            TagBuilder editorrow = new TagBuilder("td");


            TagBuilder div = new TagBuilder("div");
            div.Attributes["id"] = id + "_" + name + "_edit";
            div.Attributes["style"] = "display:none;";
            div.Attributes["class"] = "form-group insideEditor";
            TagBuilder divsub = new TagBuilder("div");
            divsub.Attributes["class"] = "col-md-12";

            IHtmlContent editor = HtmlHelperEditorExtensions.EditorFor(htmlHelper, expression);
            string e = editor.GetString();

            divsub.InnerHtml.AppendHtml(e.Replace("class=\"text-box single-line\"", "class=\"text-box single-line form-control\"").Replace("id=\"", "id=\"" + id + "_"));
            div.InnerHtml.AppendHtml(divsub.GetString());

            TagBuilder div2 = new TagBuilder("div");
            div2.Attributes["id"] = id + "_" + name + "_text";
            div2.Attributes["class"] = "insideText";
            div2.InnerHtml.AppendHtml(display.GetString().Replace("id=\"a_", "id=\"" + id + "_showtext_"));

            editorrow.InnerHtml.AppendHtml(div.GetString() + " " + div2.GetString());

            row.InnerHtml.AppendHtml(editorrow);

            return new HtmlString(row.GetString());


        }

        public static HtmlString MyPrAEditorRowFor<TModel, TProperty>(
    this IHtmlHelper<TModel> htmlHelper,
    Expression<Func<TModel, TProperty>> expression, string ikclass, string id)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new InvalidOperationException("Expression must be a member expression");

            string name = memberExpression.Member.Name;


            IHtmlContent display = HtmlHelperDisplayExtensions.DisplayFor(htmlHelper, expression);

            TagBuilder row = new TagBuilder("tr");
            TagBuilder desc = new TagBuilder("td");
            desc.Attributes["style"] = "font-weight:bold; font-size:1em;";
            desc.InnerHtml.Append(ikclass); ;
            row.InnerHtml.AppendHtml(desc);
            TagBuilder space = new TagBuilder("td");
            space.InnerHtml.AppendHtml("&nbsp;&nbsp;");
            row.InnerHtml.AppendHtml(space);

            TagBuilder editorrow = new TagBuilder("td");

            TagBuilder div = new TagBuilder("div");
            div.Attributes["id"] = id.ToString() + "_" + name + "_edit";
            div.Attributes["style"] = "display:none;";
            div.Attributes["class"] = "form-group insidePraEditor";
            TagBuilder divsub = new TagBuilder("div");
            divsub.Attributes["class"] = "col-md-12";

            IHtmlContent editor = HtmlHelperEditorExtensions.EditorFor(htmlHelper, expression);
            string e = editor.GetString();

            divsub.InnerHtml.AppendHtml(e.Replace("class=\"text-box single-line\"", "class=\"text-box single-line form-control\"").Replace("id=\"", "id=\"" + id + "_"));
            div.InnerHtml.AppendHtml(divsub.GetString());

            TagBuilder div2 = new TagBuilder("div");
            div2.Attributes["id"] = id.ToString() + "_" + name + "_text";
            div2.Attributes["class"] = "insidePraText";
            div2.InnerHtml.AppendHtml(display.GetString().Replace("id=\"a_", "id=\"" + id + "_showtext_"));
            editorrow.Attributes["style"] = "width:90px;";
            editorrow.InnerHtml.AppendHtml(div.GetString() + " " + div2.GetString());

            row.InnerHtml.AppendHtml(editorrow);

            return new HtmlString(row.GetString());


        }

        public static HtmlString ObjetRowFor<TModel, TProperty>(
    this IHtmlHelper<TModel> htmlHelper,
    Expression<Func<TModel, TProperty>> expression, MiResiliencia.Models.ObjectparameterViewModel ovm)
        {
            MiResiliencia.Models.MappedObject m = ovm.MappedObject;

            string id = m.ID.ToString();
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new InvalidOperationException("Expression must be a member expression");

            string name = memberExpression.Member.Name;
            string color = "";

            // Show the row only if there is an entry for the property
            MiResiliencia.Models.ObjectparameterHasProperties p = ovm.HasProperties.Where(pro => pro.Property == name).FirstOrDefault();
            if (p != null)
            {
                if (p.isOptional == false)
                {
                    // check if the value differs from the mother object
                    if ((m.Objectparameter.IsStandard == false) && (m.Objectparameter.MotherOtbjectparameter != null))
                    {
                        if ((m.Objectparameter.MotherOtbjectparameter.GetType().GetProperty(name) != null) && (m.Objectparameter.MotherOtbjectparameter.GetType().GetProperty(name).GetValue(m.Objectparameter.MotherOtbjectparameter, null) != null))
                        {
                            if (!m.Objectparameter.MotherOtbjectparameter.GetType().GetProperty(name).GetValue(m.Objectparameter.MotherOtbjectparameter, null).Equals(m.Objectparameter.GetType().GetProperty(name).GetValue(m.Objectparameter, null)))
                            {
                                color = "color:red;";
                            }
                        }
                    }
                }

                // create the tr content


                DisplayNameAttribute? displayNameAttr =
                (DisplayNameAttribute?)Attribute.GetCustomAttribute(
                    htmlHelper.GetType(), typeof(DisplayNameAttribute));
                string displayname = displayNameAttr == null ?
                      htmlHelper.DisplayNameFor(expression) :
                      displayNameAttr.DisplayName.ToString();
                IHtmlContent display = HtmlHelperDisplayExtensions.DisplayFor(htmlHelper, expression);

                if (displayname == "Ocupaci&#243;n de personas por piso")
                {
                    if (ovm.MappedObject.Objectparameter.ObjectClass != null)
                    {
                        if (ovm.MappedObject.Objectparameter.ObjectClass.Name == "Objetos especiales")
                        {
                            displayname = "Ocupación de personas por edificio";
                        }
                        else if (ovm.MappedObject.Objectparameter.ObjectClass.Name == "Infraestructura")
                        {
                            displayname = "Ocupación de personas por vehículo";
                        }
                    }
                }

                TagBuilder row = new TagBuilder("tr");
                TagBuilder desc = new TagBuilder("td");
                desc.Attributes["style"] = "font-weight:bold;";
                desc.InnerHtml.AppendHtml(displayname);
                row.InnerHtml.AppendHtml(desc);
                TagBuilder editorrow = new TagBuilder("td");
                editorrow.Attributes["style"] = color;


                TagBuilder div = new TagBuilder("div");
                div.Attributes["id"] = id.ToString() + "_" + name + "_edit";
                div.Attributes["style"] = "display:none;";
                div.Attributes["class"] = "form-group insideEditor";
                TagBuilder divsub = new TagBuilder("div");
                divsub.Attributes["class"] = "col-md-12";

                IHtmlContent editor = HtmlHelperEditorExtensions.EditorFor(htmlHelper, expression);
                string e = editor.GetString();

                divsub.InnerHtml.AppendHtml(e.Replace("class=\"text-box single-line\"", "class=\"text-box single-line form-control\"").Replace("id=\"", "id=\"" + id + "_"));
                div.InnerHtml.AppendHtml(divsub);

                TagBuilder div2 = new TagBuilder("div");
                div2.Attributes["id"] = id.ToString() + "_" + name + "_text";
                div2.Attributes["class"] = "insideText";
                div2.InnerHtml.AppendHtml(display.GetString().Replace("id=\"a_", "id=\"" + id + "_showtext_"));
                    
                editorrow.InnerHtml.AppendHtml(div.GetString() + " " + div2.GetString());

                row.InnerHtml.AppendHtml(editorrow.GetString());

                return new HtmlString(row.GetString());
            }
            else return new HtmlString("");


        }


        public static HtmlString ObjetRowFor<TModel, TProperty>(
    this IHtmlHelper<TModel> htmlHelper,
    Expression<Func<TModel, TProperty>> expression, MiResiliencia.Models.MultipleObjectparameterViewModel ovm)
        {
            MiResiliencia.Models.MappedObject m = ovm.MappedObjects.First();

            string id = string.Join("_", ovm.MappedObjects.Select(m => m.ID));
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new InvalidOperationException("Expression must be a member expression");

            string name = memberExpression.Member.Name;
            string color = "";

            // Show the row only if there is an entry for the property
            MiResiliencia.Models.ObjectparameterHasProperties p = ovm.HasProperties.Where(pro => pro.Property == name).FirstOrDefault();
            if (p != null)
            {
                if (p.isOptional == false)
                {
                    // check if the value differs from the mother object
                    if ((m.Objectparameter.IsStandard == false) && (m.Objectparameter.MotherOtbjectparameter != null))
                    {
                        if ((m.Objectparameter.MotherOtbjectparameter.GetType().GetProperty(name) != null) && (m.Objectparameter.MotherOtbjectparameter.GetType().GetProperty(name).GetValue(m.Objectparameter.MotherOtbjectparameter, null) != null))
                        {
                            if (!m.Objectparameter.MotherOtbjectparameter.GetType().GetProperty(name).GetValue(m.Objectparameter.MotherOtbjectparameter, null).Equals(m.Objectparameter.GetType().GetProperty(name).GetValue(m.Objectparameter, null)))
                            {
                                color = "color:red;";
                            }
                        }
                    }
                }

                // create the tr content


                DisplayNameAttribute? displayNameAttr =
                (DisplayNameAttribute?)Attribute.GetCustomAttribute(
                    htmlHelper.GetType(), typeof(DisplayNameAttribute));
                string displayname = displayNameAttr == null ?
                      htmlHelper.DisplayNameFor(expression) :
                      displayNameAttr.DisplayName.ToString();
                IHtmlContent display = HtmlHelperDisplayExtensions.DisplayFor(htmlHelper, expression);

                if (displayname == "Ocupaci&#243;n de personas por piso")
                {
                    if (ovm.MappedObjects.First().Objectparameter.ObjectClass != null)
                    {
                        if (ovm.MappedObjects.First().Objectparameter.ObjectClass.Name == "Objetos especiales")
                        {
                            displayname = "Ocupación de personas por edificio";
                        }
                        else if (ovm.MappedObjects.First().Objectparameter.ObjectClass.Name == "Infraestructura")
                        {
                            displayname = "Ocupación de personas por vehículo";
                        }
                    }
                }

                TagBuilder row = new TagBuilder("tr");
                TagBuilder desc = new TagBuilder("td");
                desc.Attributes["style"] = "font-weight:bold;";
                desc.InnerHtml.AppendHtml(displayname);
                row.InnerHtml.AppendHtml(desc);
                TagBuilder editorrow = new TagBuilder("td");
                editorrow.Attributes["style"] = color;


                TagBuilder div = new TagBuilder("div");
                div.Attributes["id"] = id.ToString() + "_" + name + "_edit";
                div.Attributes["style"] = "display:none;";
                div.Attributes["class"] = "form-group insideEditor";
                TagBuilder divsub = new TagBuilder("div");
                divsub.Attributes["class"] = "col-md-12";

                IHtmlContent editor = HtmlHelperEditorExtensions.EditorFor(htmlHelper, expression);
                string e = editor.GetString();

                divsub.InnerHtml.AppendHtml(e.Replace("class=\"text-box single-line\"", "class=\"text-box single-line form-control\"").Replace("id=\"", "id=\"" + id + "_"));
                div.InnerHtml.AppendHtml(divsub);

                TagBuilder div2 = new TagBuilder("div");
                div2.Attributes["id"] = id.ToString() + "_" + name + "_text";
                div2.Attributes["class"] = "insideText";
                div2.InnerHtml.AppendHtml(display.GetString().Replace("id=\"a_", "id=\"" + id + "_showtext_"));

                editorrow.InnerHtml.AppendHtml(div.GetString() + " " + div2.GetString());

                row.InnerHtml.AppendHtml(editorrow.GetString());

                return new HtmlString(row.GetString());
            }
            else return new HtmlString("");


        }


        public static TAttribute GetAttribute<TAttribute>(this Enum enumValue)
                where TAttribute : Attribute
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<TAttribute>();
        }

        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()
                            .GetName();
        }

        
    }
}
