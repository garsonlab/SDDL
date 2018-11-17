using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using SDDL;
using Type = SDDL.Type;

public class CSharp : Target
{
    protected readonly string ns;
    protected bool consting;

    public CSharp(string ns)
    {
        this.ns = ns;
        this.consting = false;
    }

    public Encoding Encoding
    {
        get { return new UTF8Encoding(false); }
    }

    public string NewLine
    {
        get { return "\n"; }
    }

    public virtual void Prepare(StreamWriter writer)
    {
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using SDDL;");
        writer.WriteLine();
        if (!string.IsNullOrEmpty(ns))
        {
            writer.WriteLine("namespace {0}", ns);
            writer.WriteLine("{");
        }
    }

    public virtual void Flush(StreamWriter writer)
    {
        if (consting)
        {
            consting = false;
            writer.WriteLine((string.IsNullOrEmpty(ns) ? "" : "\t") + "}");
        }
        if (!string.IsNullOrEmpty(ns))
            writer.WriteLine("}");
    }

    public virtual void Value(StreamWriter writer, string name, bool b)
    {
        string indent = string.IsNullOrEmpty(ns) ? "" : "\t";
        if (!consting)
        {
            consting = true;
            writer.WriteLine(indent + "public static partial class Const");
            writer.WriteLine(indent + "{");
        }
        writer.WriteLine(indent + "\tpublic const bool {0} = {1};", name, ToString(b));
    }

    public virtual void Value(StreamWriter writer, string name, int i)
    {
        string indent = string.IsNullOrEmpty(ns) ? "" : "\t";
        if (!consting)
        {
            consting = true;
            writer.WriteLine(indent + "public static partial class Const");
            writer.WriteLine(indent + "{");
        }
        writer.WriteLine(indent + "\tpublic const int {0} = {1};", name, ToString(i));
    }

    public virtual void Value(StreamWriter writer, string name, double d)
    {
        string indent = string.IsNullOrEmpty(ns) ? "" : "\t";
        if (!consting)
        {
            consting = true;
            writer.WriteLine(indent + "public static partial class Const");
            writer.WriteLine(indent + "{");
        }
        writer.WriteLine(indent + "\tpublic const double {0} = {1};", name, ToString(d));
    }

    public virtual void Value(StreamWriter writer, string name, string s)
    {
        string indent = string.IsNullOrEmpty(ns) ? "" : "\t";
        if (!consting)
        {
            consting = true;
            writer.WriteLine(indent + "public static partial class Const");
            writer.WriteLine(indent + "{");
        }
        writer.WriteLine(indent + "\tpublic const string {0} = {1};", name, ToString(s));
    }

    public virtual void Message(StreamWriter writer, string name, params Entry[] entries)
    {
        string indent = string.IsNullOrEmpty(ns) ? "" : "\t";
        if (consting)
        {
            consting = false;
            writer.WriteLine(indent + "}");
        }
        WriteMessage(writer, false, indent, name, entries);
    }

    public virtual void Typedef(StreamWriter writer, string name, params Alias[] aliases)
    {
        string indent = string.IsNullOrEmpty(ns) ? "" : "\t";

        writer.WriteLine(indent + "public abstract class {0}<T> : MSG<{0}.Value, T> where T : Output", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tprotected {0}(Func<Protocol.Format> acquire, Action<Protocol.Format> release) : base(acquire, release)", name);
        writer.WriteLine(indent + "\t{");
        List<string> types = new List<string>();
        List<string> wraps = new List<string>();
        for (int i = 0; i < aliases.Length; ++i)
        {
            Alias alias = aliases[i];
            types.Add("T");
            if (alias.Type.HasValue)
                types.Add(GetType(alias.Type.Value, alias.TypeEx));
            string type = string.Format("{0}.Wrap<{1}>", name, string.Join(", ", types));
            types.Clear();
            types.Add("this");
            types.Add(alias.Place.ToString());
            if (alias.Type.HasValue)
            {
                types.Add(string.Format("{0}.InputWrap.{1}", name, alias.Name));
                types.Add(string.Format("{0}.VerifyWrap.{1}", name, alias.Name));
                types.Add(string.Format("{0}.OutputWrap.{1}", name, alias.Name));
            }
            writer.WriteLine(indent + "\t\t{0} = new {1}({2});", alias.Name, type, string.Join(", ", types));
            types.Clear();
            wraps.Add(string.Format("public readonly {0} {1};", type, alias.Name));
        }
        writer.WriteLine(indent + "\t}");
        foreach (string wrap in wraps)
            writer.WriteLine(indent + "\t" + wrap);
        writer.WriteLine(indent + "}");

        #region 生成请求参数封装函数
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic static class InputWrap");
        writer.WriteLine(indent + "\t{");
        for (int i = 0; i < aliases.Length; ++i)
        {
            Alias alias = aliases[i];
            if (alias.Type.HasValue)
            {
                writer.WriteLine(indent + "\t\tpublic static readonly Func<{0}, {1}.Value> {2} = value => new {1}.Value {{{2} = value}};",
                                GetType(alias.Type.Value, alias.TypeEx), name, alias.Name);
            }
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
        #endregion

        #region 生成返回值验证函数
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic static class VerifyWrap");
        writer.WriteLine(indent + "\t{");
        for (int i = 0; i < aliases.Length; ++i)
        {
            Alias alias = aliases[i];
            if (alias.Type.HasValue)
            {
                writer.WriteLine(
                    indent + "\t\tpublic static readonly Func<{0}.Value, bool> {1} = value => value.{1} != null;", name,
                    alias.Name);
            }
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
        #endregion

        #region 生成返回值封装函数
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic static class OutputWrap");
        writer.WriteLine(indent + "\t{");
        for (int i = 0; i < aliases.Length; ++i)
        {
            Alias alias = aliases[i];
            if (alias.Type.HasValue)
            {
                writer.WriteLine(indent + "\t\tpublic static readonly Func<{0}.Value, {1}> {2} = value => value.{2}{3};", name,
                                GetType(alias.Type.Value, alias.TypeEx), alias.Name, alias.Type.Value == SDDL.Type.String || alias.Type.Value == SDDL.Type.Other ? "" : ".Value");
            }
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
        #endregion

        TypedefWriter typedef = CreateTypedefWriter(name);
        typedef.Write(writer, indent, false);
        typedef.Write(writer, indent, true);
        TypedefValue(writer, indent, name, aliases);

        writer.WriteLine(indent + "public static partial class Method");
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic enum {0}", name);
        writer.WriteLine(indent + "\t{");
        for (int i = 0; i < aliases.Length; ++i)
        {
            Alias alias = aliases[i];
            writer.WriteLine(indent + "\t\t{0} = {1},", alias.Name, alias.Place);
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
    }

    public virtual TypedefWriter CreateTypedefWriter(string name)
    {
        return new TypedefWriter(name);
    }

    public class TypedefWriter
    {
        protected readonly string name;

        public TypedefWriter(string name)
        {
            this.name = name;
        }

        public virtual void WriteInit(StreamWriter writer, string indent, bool withvalue)
        {
            writer.WriteLine(indent + "private readonly {0}<T> Self;", name);
            writer.WriteLine(indent + "private readonly int Place;");
            if (withvalue)
            {
                writer.WriteLine(indent + "private readonly Func<TValue, {0}.Value> Input;", name);
                writer.WriteLine(indent + "private readonly Func<{0}.Value, bool> Verify;", name);
                writer.WriteLine(indent + "private readonly Func<{0}.Value, TValue> Output;", name);
            }
            writer.WriteLine(indent + "public Wrap({0}<T> self, int place{1})", name,
                            withvalue ? string.Format(", Func<TValue, {0}.Value> input, Func<{0}.Value, bool> verify, Func<{0}.Value, TValue> output", name) : "");
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "\tSelf = self;");
            writer.WriteLine(indent + "\tPlace = place;");
            if (withvalue)
            {
                writer.WriteLine(indent + "\tInput = input;");
                writer.WriteLine(indent + "\tVerify = verify;");
                writer.WriteLine(indent + "\tOutput = output;");
            }
            writer.WriteLine(indent + "}");
        }

        public virtual void WriteSend(StreamWriter writer, string indent, bool withvalue)
        {
            writer.WriteLine(indent + "public void Send({0})", withvalue ? "TValue value" : "");
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "\tSelf.Output(Place, {0});", withvalue ? "Input(value)" : "null");
            writer.WriteLine(indent + "}");
        }

        public virtual void WriteBind(StreamWriter writer, string indent, bool withvalue)
        {
            writer.WriteLine(indent + "public void Bind(Action{0} callback)", withvalue ? "<TValue>" : "");
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "\tif (callback == null)");
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\tSelf.Listen(Place, null);");
            writer.WriteLine(indent + "\t}");
            writer.WriteLine(indent + "\telse");
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\tSelf.Listen(Place, value =>");
            writer.WriteLine(indent + "\t\t{");
            if (withvalue)
            {
                writer.WriteLine(indent + "\t\t\tif (value == null || !Verify(value.Value))");
                writer.WriteLine(indent + "\t\t\t\treturn false;");
                writer.WriteLine(indent + "\t\t\tcallback(Output(value.Value));");
            }
            else
            {
                writer.WriteLine(indent + "\t\t\tcallback();");
            }
            writer.WriteLine(indent + "\t\t\treturn true;");
            writer.WriteLine(indent + "\t\t});");
            writer.WriteLine(indent + "\t}");
            writer.WriteLine(indent + "}");
        }

        public virtual void WriteOnce(StreamWriter writer, string indent, bool withvalue)
        {
            writer.WriteLine(indent + "public void Once(Action{0} callback)", withvalue ? "<TValue>" : "");
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "\tif (callback == null)");
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\tSelf.ListenOnce(Place, null);");
            writer.WriteLine(indent + "\t}");
            writer.WriteLine(indent + "\telse");
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\tSelf.ListenOnce(Place, value =>");
            writer.WriteLine(indent + "\t\t{");
            if (withvalue)
            {
                writer.WriteLine(indent + "\t\t\tif (value == null || !Verify(value.Value))");
                writer.WriteLine(indent + "\t\t\t\treturn false;");
                writer.WriteLine(indent + "\t\t\tcallback(Output(value.Value));");
            }
            else
            {
                writer.WriteLine(indent + "\t\t\tcallback();");
            }
            writer.WriteLine(indent + "\t\t\treturn true;");
            writer.WriteLine(indent + "\t\t});");
            writer.WriteLine(indent + "\t}");
            writer.WriteLine(indent + "}");
        }

        public virtual void Write(StreamWriter writer, string indent, bool withvalue)
        {
            writer.WriteLine(indent + "public static partial class {0}", name);
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "\tpublic class Wrap<T{0}> where T : Output", withvalue ? ", TValue" : "");
            writer.WriteLine(indent + "\t{");
            WriteInit(writer, indent + "\t\t", withvalue);
            WriteSend(writer, indent + "\t\t", withvalue);
            WriteBind(writer, indent + "\t\t", withvalue);
            WriteOnce(writer, indent + "\t\t", withvalue);
            writer.WriteLine(indent + "\t}");
            writer.WriteLine(indent + "}");
        }
    }

    public virtual void TypedefValue(StreamWriter writer, string indent, string name, params Alias[] aliases)
    {
        List<Entry> entries = new List<Entry>(aliases.Length);
        for (int i = 0; i < aliases.Length; ++i)
        {
            Alias alias = aliases[i];
            if (alias.Type.HasValue)
            {
                entries.Add(new Entry
                {
                    Name = alias.Name,
                    Type = alias.Type.Value,
                    TypeEx = alias.TypeEx,
                    Option = EntryOption.Option,
                    Place = alias.Place,
                });
            }
        }
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        WriteMessage(writer, true, indent + "\t", "Value", entries.ToArray());
        writer.WriteLine(indent + "}");
    }

    public virtual void RPC(StreamWriter writer, string name, params Call[] calls)
    {
        string indent = string.IsNullOrEmpty(ns) ? "" : "\t";
        writer.WriteLine(indent + "public static partial class Request");
        writer.WriteLine(indent + "{");
        Request(writer, indent + "\t", name, calls);
        RequestValue(writer, indent + "\t", name, calls);
        writer.WriteLine(indent + "}");

        writer.WriteLine(indent + "public static partial class Response");
        writer.WriteLine(indent + "{");
        Response(writer, indent + "\t", name, calls);
        ResponseValue(writer, indent + "\t", name, calls);
        writer.WriteLine(indent + "}");

        writer.WriteLine(indent + "public static partial class Method");
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic enum {0}", name);
        writer.WriteLine(indent + "\t{");
        for (int i = 0; i < calls.Length; ++i)
        {
            Call call = calls[i];
            writer.WriteLine(indent + "\t\t{0} = {1},", call.Name, call.Place);
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
    }

    public virtual void Request(StreamWriter writer, string indent, string name, params Call[] calls)
    {
        writer.WriteLine(indent + "public abstract class {0}<T> : RPC.Request<{0}.Value, Response.{0}.Value, T> where T : Output", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tprotected {0}(Func<Protocol.Format> acquire, Action<Protocol.Format> release) : base(acquire, release) {{}}", name);
        List<string> types = new List<string>();
        for (int i = 0; i < calls.Length; ++i)
        {
            Call call = calls[i];
            if (call.RequestType.HasValue)
                types.Add(GetType(call.RequestType.Value, call.RequestTypeEx) + " request");
            types.Add("Action<string" +
                    (call.ResponseType.HasValue ? ", " + GetOptionType(call.ResponseType.Value, call.ResponseTypeEx) : "") +
                    "> callback");
            writer.WriteLine(indent + "\tpublic void {0}({1})", call.Name, string.Join(", ", types));
            types.Clear();
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\tCall({0}, {1}, (error, response) =>", call.Place,
                            call.RequestType.HasValue ? string.Format("new {0}.Value {{{1} = request}}", name, call.Name) : "null");
            writer.WriteLine(indent + "\t\t{");
            if (call.ResponseType.HasValue)
            {
                writer.WriteLine(indent + "\t\t\tif (error == null)");
                writer.WriteLine(indent + "\t\t\t{");
                writer.WriteLine(indent + "\t\t\t\tif (response == null || response.Value.{0} == null)", call.Name);
                writer.WriteLine(indent + "\t\t\t\t\treturn false;");
                writer.WriteLine(indent + "\t\t\t}");
                writer.WriteLine(indent + "\t\t\tcallback(error, response != null ? response.Value.{0} : null);", call.Name);
            }
            else
            {
                writer.WriteLine(indent + "\t\t\tcallback(error);");
            }
            writer.WriteLine(indent + "\t\t\treturn true;");
            writer.WriteLine(indent + "\t\t});");
            writer.WriteLine(indent + "\t}");
        }
        writer.WriteLine(indent + "}");
    }

    public virtual void Response(StreamWriter writer, string indent, string name, params Call[] calls)
    {
        List<string> types = new List<string>();
        Dictionary<int, List<string>> dispatch = new Dictionary<int, List<string>>();
        writer.WriteLine(indent + "public abstract class {0}<T> : RPC.Response<Request.{0}.Value, {0}.Value, T>", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tprotected {0}(Func<Protocol.Format> acquire, Action<Protocol.Format> release) : base(acquire, release) {{}}", name);
        types.Clear();
        dispatch.Clear();
        for (int i = 0; i < calls.Length; ++i)
        {
            Call call = calls[i];
            types.Add("T");
            if (call.RequestType.HasValue)
                types.Add(GetType(call.RequestType.Value, call.RequestTypeEx));
            if (call.ResponseType.HasValue)
                types.Add("Action<string, " + GetOptionType(call.ResponseType.Value, call.ResponseTypeEx) + ">");
            else
                types.Add("Action<string>");
            string str = string.Join(", ", types);
            types.Clear();
            List<string> cases = new List<string>();
            dispatch.Add(call.Place, cases);
            writer.WriteLine(indent + "\tprivate Action<{0}> _{1};", str, call.Name);
            writer.WriteLine(indent + "\tpublic void {0}(Action<{1}> action)", call.Name, str);
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\t_{0} = action;", call.Name);
            writer.WriteLine(indent + "\t}");

            cases.Add(string.Format("if (_{0} == null)", call.Name));
            cases.Add("{");
            cases.Add("\tNotImplement(type, callback);");
            cases.Add("\treturn true;");
            cases.Add("}");
            if (call.RequestType.HasValue)
            {
                cases.Add(string.Format("if (request == null || request.Value.{0} == null)", call.Name));
                cases.Add("\tbreak;");
                if (call.ResponseType.HasValue)
                {
                    cases.Add(string.Format(
                        "_{0}(session, request.Value.{0}{2}, (error, result) => callback(error, result != null ? new {1}.Value?(new {1}.Value {{{0} = result}}) : null));",
                        call.Name, name, call.RequestType.Value == SDDL.Type.String || call.RequestType.Value == SDDL.Type.Other ? "" : ".Value"));
                }
                else
                {
                    cases.Add(string.Format(
                        "_{0}(session, request.Value.{0}{1}, error => callback(error, null));",
                        call.Name, call.RequestType.Value == SDDL.Type.String || call.RequestType.Value == SDDL.Type.Other ? "" : ".Value"));
                }
            }
            else
            {
                if (call.ResponseType.HasValue)
                {
                    cases.Add(string.Format(
                        "_{0}(session, (error, result) => callback(error, result != null ? new {1}.Value?(new {1}.Value {{{0} = result}}) : null));",
                        call.Name, name));
                }
                else
                {
                    cases.Add(string.Format(
                        "_{0}(session, error => callback(error, null));",
                        call.Name));
                }
            }
        }
        writer.WriteLine(indent +
                        "\tprotected override bool Dispatch(int type, Request.{0}.Value? request, T session, Action<string, {0}.Value?> callback)", name);
        writer.WriteLine(indent + "\t{");
        writer.WriteLine(indent + "\t\tswitch (type)");
        writer.WriteLine(indent + "\t\t{");
        {
            int[] switchs = dispatch.Keys.ToArray();
            Array.Sort(switchs);
            for (int i = 0; i < switchs.Length; ++i)
            {
                writer.WriteLine(indent + "\t\tcase {0}:", switchs[i]);
                foreach (string str in dispatch[switchs[i]])
                    writer.WriteLine(indent + "\t\t\t" + str);
                writer.WriteLine(indent + "\t\t\treturn true;");
            }
        }
        writer.WriteLine(indent + "\t\t}");
        writer.WriteLine(indent + "\t\treturn false;");
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
    }

    public virtual void RequestValue(StreamWriter writer, string indent, string name, params Call[] calls)
    {
        List<Entry> entries = new List<Entry>(calls.Length);
        for (int i = 0; i < calls.Length; ++i)
        {
            Call call = calls[i];
            if (call.RequestType.HasValue)
            {
                entries.Add(new Entry
                {
                    Name = call.Name,
                    Type = call.RequestType.Value,
                    TypeEx = call.RequestTypeEx,
                    Option = EntryOption.Option,
                    Place = call.Place,
                });
            }
        }
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        WriteMessage(writer, true, indent + "\t", "Value", entries.ToArray());
        writer.WriteLine(indent + "}");
    }

    public virtual void ResponseValue(StreamWriter writer, string indent, string name, params Call[] calls)
    {
        List<Entry> entries = new List<Entry>(calls.Length);
        for (int i = 0; i < calls.Length; ++i)
        {
            Call call = calls[i];
            if (call.ResponseType.HasValue)
            {
                entries.Add(new Entry
                {
                    Name = call.Name,
                    Type = call.ResponseType.Value,
                    TypeEx = call.ResponseTypeEx,
                    Option = EntryOption.Option,
                    Place = call.Place,
                });
            }
        }
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        WriteMessage(writer, true, indent + "\t", "Value", entries.ToArray());
        writer.WriteLine(indent + "}");
    }

    public static string GetType(SDDL.Type type, string ex)
    {
        switch (type)
        {
            case SDDL.Type.Bool:
                return "bool";
            case SDDL.Type.Int:
                return "int";
            case SDDL.Type.Float:
                return "double";
            case SDDL.Type.String:
                return "string";
            default:
                return ex;
        }
    }

    public static string GetOptionType(SDDL.Type type, string ex)
    {
        if (type == SDDL.Type.String || type == SDDL.Type.Other)
            return GetType(type, ex);
        return GetType(type, ex) + "?";
    }

    public static void WriteMessage(StreamWriter writer, bool valuetype, string indent, string name, Entry[] entries)
    {
        writer.WriteLine(indent + "public {0} {1} : Protocol.Value", valuetype ? "struct" : "class", name);
        writer.WriteLine(indent + "{");
        Dictionary<string, string> fieldnames = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            string typename = GetType(entry.Type, entry.TypeEx);
            string fieldname = entry.Name;
            string defaultvalue = "";
            if (entry.Option == EntryOption.Require)
            {
                if (entry.Type == SDDL.Type.String)
                {
                    defaultvalue = "\"\"";
                    writer.WriteLine(indent + "\tpublic {0} {1}", typename, fieldname);
                    fieldname = "__" + fieldname + "__";
                    writer.WriteLine(indent + "\t{");
                    writer.WriteLine(indent + "\t\tget {{ return {0}; }}", fieldname);
                    writer.WriteLine(indent + "\t\tset {{ {0} = value ?? \"\"; }}", fieldname);
                    writer.WriteLine(indent + "\t}");
                }
                else if (entry.Type == SDDL.Type.Other)
                {
                    defaultvalue = string.Format("new {0}()", typename);
                    writer.WriteLine(indent + "\tpublic {0} {1}", typename, fieldname);
                    fieldname = "__" + fieldname + "__";
                    writer.WriteLine(indent + "\t{");
                    writer.WriteLine(indent + "\t\tget {{ return {0}; }}", fieldname);
                    writer.WriteLine(indent + "\t}");
                }
            }
            if (entry.Option == EntryOption.Array)
            {
                typename = string.Format("List<{0}>", typename);
                defaultvalue = string.Format("new {0}()", typename);
                writer.WriteLine(indent + "\tpublic {0} {1}", typename, fieldname);
                fieldname = "__" + fieldname + "__";
                writer.WriteLine(indent + "\t{");
                writer.WriteLine(indent + "\t\tget {{ return {0}; }}", fieldname);
                writer.WriteLine(indent + "\t}");
            }
            else if (entry.Option == EntryOption.Table)
            {
                typename = string.Format("Dictionary<int, {0}>", typename);
                defaultvalue = string.Format("new {0}()", typename);
                writer.WriteLine(indent + "\tpublic {0} {1}", typename, fieldname);
                fieldname = "__" + fieldname + "__";
                writer.WriteLine(indent + "\t{");
                writer.WriteLine(indent + "\t\tget {{ return {0}; }}", fieldname);
                writer.WriteLine(indent + "\t}");
            }
            else if (entry.Option == EntryOption.Option)
            {
                switch (entry.Type)
                {
                    case SDDL.Type.Bool:
                    case SDDL.Type.Int:
                    case SDDL.Type.Float:
                        typename = string.Format("{0}?", typename);
                        break;
                }
            }
            writer.WriteLine(indent + "\t{0} {1} {2}{3};", fieldname == entry.Name ? "public" : "private", typename, fieldname, string.IsNullOrEmpty(defaultvalue) ? "" : string.Format(" = {0}", defaultvalue));
            fieldnames[entry.Name] = fieldname;
        }

        writer.WriteLine(indent + "\tpublic bool Read(Protocol.Format format, int key)");
        writer.WriteLine(indent + "\t{");
        writer.WriteLine(indent + "\t\tswitch (key)");
        writer.WriteLine(indent + "\t\t{");
        foreach (var entry in entries)
        {
            writer.Write(indent + "\t\tcase ");
            writer.Write(entry.Place);
            writer.WriteLine(":");
            writer.WriteLine(indent + "\t\t\treturn format.ReadValue(ref {0});", fieldnames[entry.Name]);
        }
        writer.WriteLine(indent + "\t\tdefault:");
        writer.WriteLine(indent + "\t\t\treturn format.Skip();");
        writer.WriteLine(indent + "\t\t}");
        writer.WriteLine(indent + "\t}");

        writer.WriteLine(indent + "\tpublic void Write(Protocol.Format format)");
        writer.WriteLine(indent + "\t{");
        foreach (var entry in entries)
        {
            writer.WriteLine(indent + "\t\tformat.WriteValue({0}, {1});", entry.Place, fieldnames[entry.Name]);
        }
        writer.WriteLine(indent + "\t}");

        writer.WriteLine(indent + "\tpublic void Reset()");
        writer.WriteLine(indent + "\t{");
        foreach (var entry in entries)
        {
            if (entry.Option == EntryOption.Require)
            {
                if (entry.Value != null)
                {
                    string v = "null";
                    bool b;
                    int i;
                    double d;
                    string s;
                    if (entry.Value.TryToBoolean(out b))
                        v = ToString(b);
                    else if (entry.Value.TryToInteger(out i))
                        v = ToString(i);
                    else if (entry.Value.TryToFloat(out d))
                        v = ToString(d);
                    else if (entry.Value.TryToString(out s))
                        v = ToString(s);
                    writer.WriteLine(indent + "\t\t{0} = {1};", fieldnames[entry.Name], v);
                }
                else
                {
                    writer.WriteLine(indent + "\t\t{0}.Reset();", fieldnames[entry.Name]);
                }
            }
            else if (entry.Option == EntryOption.Option)
            {
                writer.WriteLine(indent + "\t\t{0} = null;", fieldnames[entry.Name]);
            }
            else
            {
                writer.WriteLine(indent + "\t\t{0}.Clear();", fieldnames[entry.Name]);
            }
        }
        writer.WriteLine(indent + "\t}");

        writer.WriteLine(indent + "\tpublic string Keys(int key)");
        writer.WriteLine(indent + "\t{");
        writer.WriteLine(indent + "\t\tswitch (key)");
        writer.WriteLine(indent + "\t\t{");
        foreach (var entry in entries)
        {
            writer.Write(indent + "\t\tcase ");
            writer.Write(entry.Place);
            writer.WriteLine(":");
            writer.WriteLine(indent + "\t\t\treturn \"{0}\";", entry.Name);
        }
        writer.WriteLine(indent + "\t\tdefault:");
        writer.WriteLine(indent + "\t\t\treturn null;");
        writer.WriteLine(indent + "\t\t}");
        writer.WriteLine(indent + "\t}");

        if (!valuetype)
        {
            writer.WriteLine(indent + "\tpublic void Assign({0} rhs)", name);
            writer.WriteLine(indent + "\t{");
            foreach (var entry in entries)
            {
                if (entry.Option == EntryOption.Array || entry.Option == EntryOption.Table)
                {
                    writer.WriteLine(indent + "\t\t{0} = rhs.{0};", fieldnames[entry.Name]);
                }
                else
                {
                    if (entry.Type == Type.Other)
                    {
                        writer.WriteLine(indent + "\t\tif (rhs.{0} == null)", fieldnames[entry.Name]);
                        writer.WriteLine(indent + "\t\t{");
                        writer.WriteLine(indent + "\t\t\t{0} = null;", fieldnames[entry.Name]);
                        writer.WriteLine(indent + "\t\t}");
                        writer.WriteLine(indent + "\t\telse");
                        writer.WriteLine(indent + "\t\t{");
                        writer.WriteLine(indent + "\t\t\tif ({0} == null)", fieldnames[entry.Name]);
                        writer.WriteLine(indent + "\t\t\t\t{0} = new {1}();", fieldnames[entry.Name], GetType(entry.Type, entry.TypeEx));
                        writer.WriteLine(indent + "\t\t\t{0}.Assign(rhs.{0});", fieldnames[entry.Name]);
                        writer.WriteLine(indent + "\t\t}");
                    }
                    else
                    {
                        writer.WriteLine(indent + "\t\t{0} = rhs.{0};", fieldnames[entry.Name]);
                    }
                }
            }
            writer.WriteLine(indent + "\t}");
        }

        writer.WriteLine(indent + "}");
    }

    public static string ToString(bool b)
    {
        return b ? "true" : "false";
    }

    public static string ToString(int i)
    {
        return i.ToString(CultureInfo.InvariantCulture);
    }

    public static string ToString(double d)
    {
        return d.ToString("g", CultureInfo.InvariantCulture);
    }

    public static string ToString(string s)
    {
        StringBuilder builder = new StringBuilder(s.Length + 2);
        builder.Append('"');
        for (int i = 0; i < s.Length; ++i)
        {
            char c = s[i];
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                case '\a':
                    builder.Append("\\a");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\v':
                    builder.Append("\\v");
                    break;
                default:
                    if (c < 32)
                        builder.AppendFormat("\\u{0}", ((int)c).ToString("X4"));
                    else
                        builder.Append(c);
                    break;
            }
        }
        builder.Append('"');
        return builder.ToString();
    }
}