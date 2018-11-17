using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SDDL;

public class CSharpAsync : CSharp
{
    public CSharpAsync(string ns) : base(ns) { }

    public override void Prepare(StreamWriter writer)
    {
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Threading.Tasks;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using System.Runtime.CompilerServices;");
        writer.WriteLine("using SDDL;");
        writer.WriteLine();
        if (!string.IsNullOrEmpty(ns))
        {
            writer.WriteLine("namespace {0}", ns);
            writer.WriteLine("{");
        }
    }

    public override TypedefWriter CreateTypedefWriter(string name)
    {
        return new TypedefWriterAsync(name);
    }

    private class TypedefWriterAsync : TypedefWriter
    {
        public TypedefWriterAsync(string name) : base(name) { }

        public override void WriteOnce(StreamWriter writer, string indent, bool withvalue)
        {
            writer.WriteLine(indent + "public Task{0} Once()", withvalue ? "<TValue>" : "");
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "\tAsyncTaskMethodBuilder{0} builder = AsyncTaskMethodBuilder{0}.Create();",
                            withvalue ? "<TValue>" : "");
            writer.WriteLine(indent + "\tSelf.ListenOnce(Place, value =>");
            writer.WriteLine(indent + "\t{");
            if (withvalue)
            {
                writer.WriteLine(indent + "\t\tif (value == null || !Verify(value.Value))");
                writer.WriteLine(indent + "\t\t\treturn false;");
                writer.WriteLine(indent + "\t\tbuilder.SetResult(Output(value.Value));");
            }
            else
            {
                writer.WriteLine(indent + "\t\tbuilder.SetResult();");
            }
            writer.WriteLine(indent + "\t\treturn true;");
            writer.WriteLine(indent + "\t});");
            writer.WriteLine(indent + "\treturn builder.Task;");
            writer.WriteLine(indent + "}");
        }
    }

    public override void Request(StreamWriter writer, string indent, string name, params Call[] calls)
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
            string result = call.ResponseType.HasValue ? "<" + GetType(call.ResponseType.Value, call.ResponseTypeEx) + ">" : "";
            writer.WriteLine(indent + "\tpublic Task{2} {0}({1})", call.Name, string.Join(", ", types), result);
            types.Clear();
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\tAsyncTaskMethodBuilder{0} builder = AsyncTaskMethodBuilder{0}.Create();", result);
            writer.WriteLine(indent + "\t\tCall({0}, {1}, (error, response) =>", call.Place,
                            call.RequestType.HasValue ? string.Format("new {0}.Value {{{1} = request}}", name, call.Name) : "null");
            writer.WriteLine(indent + "\t\t{");
            if (call.ResponseType.HasValue)
            {
                writer.WriteLine(indent + "\t\t\tif (error == null)");
                writer.WriteLine(indent + "\t\t\t{");
                writer.WriteLine(indent + "\t\t\t\tif (response == null || response.Value.{0} == null)", call.Name);
                writer.WriteLine(indent + "\t\t\t\t\treturn false;");
                writer.WriteLine(indent + "\t\t\t\tbuilder.SetResult(response.Value.{0}{1});", call.Name, call.ResponseType.Value == SDDL.Type.String || call.ResponseType.Value == SDDL.Type.Other ? "" : ".Value");
            }
            else
            {
                writer.WriteLine(indent + "\t\t\tif (error == null)");
                writer.WriteLine(indent + "\t\t\t{");
                writer.WriteLine(indent + "\t\t\t\tbuilder.SetResult();");
            }
            writer.WriteLine(indent + "\t\t\t}");
            writer.WriteLine(indent + "\t\t\telse");
            writer.WriteLine(indent + "\t\t\t{");
            writer.WriteLine(indent + "\t\t\t\tswitch (error)");
            writer.WriteLine(indent + "\t\t\t\t{");
            writer.WriteLine(indent + "\t\t\t\tcase RPC.NotImplement:");
            writer.WriteLine(indent + "\t\t\t\t\tbuilder.SetException(new NotImplementedException());");
            writer.WriteLine(indent + "\t\t\t\t\tbreak;");
            writer.WriteLine(indent + "\t\t\t\tcase RPC.FormatError:");
            writer.WriteLine(indent + "\t\t\t\t\tbuilder.SetException(new FormatException());");
            writer.WriteLine(indent + "\t\t\t\t\tbreak;");
            writer.WriteLine(indent + "\t\t\t\tdefault:");
            writer.WriteLine(indent + "\t\t\t\t\tbuilder.SetException(new Exception(error));");
            writer.WriteLine(indent + "\t\t\t\t\tbreak;");
            writer.WriteLine(indent + "\t\t\t\t}");
            writer.WriteLine(indent + "\t\t\t}");
            writer.WriteLine(indent + "\t\t\treturn true;");
            writer.WriteLine(indent + "\t\t});");
            writer.WriteLine(indent + "\t\treturn builder.Task;");
            writer.WriteLine(indent + "\t}");
        }
        writer.WriteLine(indent + "}");
    }

    public override void Response(StreamWriter writer, string indent, string name, params Call[] calls)
    {
        List<string> types = new List<string>();
        Dictionary<int, List<string>> dispatch = new Dictionary<int, List<string>>();
        writer.WriteLine(indent + "public abstract class {0}<T> : RPC.Response<Request.{0}.Value, {0}.Value, T>", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tprotected {0}(Func<Protocol.Format> acquire, Action<Protocol.Format> release) : base(acquire, release) {{}}", name);
        writer.WriteLine(indent + "\tprotected abstract string Error(Exception e);");
        types.Clear();
        dispatch.Clear();
        for (int i = 0; i < calls.Length; ++i)
        {
            Call call = calls[i];
            types.Add("T");
            if (call.RequestType.HasValue)
                types.Add(GetType(call.RequestType.Value, call.RequestTypeEx));
            string result = "";
            string asyncstr;
            string str;
            if (call.ResponseType.HasValue)
            {
                result = GetType(call.ResponseType.Value, call.ResponseTypeEx);
                types.Add(string.Format("Task<{0}>", result));
                asyncstr = string.Format("Func<{0}>", string.Join(", ", types));
                types.RemoveAt(types.Count - 1);
                types.Add(result);
                str = string.Format("Func<{0}>", string.Join(", ", types));
            }
            else
            {
                types.Add("Task");
                asyncstr = string.Format("Func<{0}>", string.Join(", ", types));
                types.RemoveAt(types.Count - 1);
                str = types.Count == 0 ? "Action" : string.Format("Action<{0}>", string.Join(", ", types));
            }
            types.Clear();
            List<string> cases = new List<string>();
            dispatch.Add(call.Place, cases);

            writer.WriteLine(indent + "\tprivate {0} _{1};", str, call.Name);
            writer.WriteLine(indent + "\tprivate {0} _{1}Async;", asyncstr, call.Name);

            writer.WriteLine(indent + "\tpublic void {0}({1} action)", call.Name, str);
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\t_{0} = action;", call.Name);
            writer.WriteLine(indent + "\t\t_{0}Async = null;", call.Name);
            writer.WriteLine(indent + "\t}");

            writer.WriteLine(indent + "\tpublic void {0}({1} action)", call.Name, asyncstr);
            writer.WriteLine(indent + "\t{");
            writer.WriteLine(indent + "\t\t_{0}Async = action;", call.Name);
            writer.WriteLine(indent + "\t\t_{0} = null;", call.Name);
            writer.WriteLine(indent + "\t}");

            types.Add("session");
            if (call.RequestType.HasValue)
                types.Add(string.Format("request.Value.{0}{1}", call.Name, call.RequestType.Value == SDDL.Type.String || call.RequestType.Value == SDDL.Type.Other ? "" : ".Value"));
            string args = string.Join(", ", types);
            types.Clear();

            cases.Add(string.Format("if (_{0} == null && _{0}Async == null)", call.Name));
            cases.Add("{");
            cases.Add("\tNotImplement(type, callback);");
            cases.Add("\treturn true;");
            cases.Add("}");
            if (call.RequestType.HasValue)
            {
                cases.Add(string.Format("if (request == null || request.Value.{0} == null)", call.Name));
                cases.Add("\tbreak;");
            }
            cases.Add(string.Format("if (_{0} != null)", call.Name));
            cases.Add("{");
            cases.Add("\ttry");
            cases.Add("\t{");
            if (result == "")
            {
                cases.Add(string.Format("\t\t_{0}({1});", call.Name, args));
                cases.Add("\t\tcallback(null, null);");
            }
            else
            {
                cases.Add(string.Format("\t\t{0} result = _{1}({2});", result, call.Name, args));
                cases.Add(string.Format("\t\tcallback(null, {0});",
                                        string.Format("new {0}.Value {{{1} = result}}", name, call.Name)));
            }
            cases.Add("\t}");
            cases.Add("\tcatch (Exception e)");
            cases.Add("\t{");
            cases.Add("\t\tcallback(Error(e), null);");
            cases.Add("\t}");
            cases.Add("}");
            cases.Add("else");
            cases.Add("{");
            cases.Add(string.Format("\t{0} task = _{1}Async({2});", result == "" ? "Task" : string.Format("Task<{0}>", result), call.Name, args));
            cases.Add("\ttask.GetAwaiter().OnCompleted(() =>");
            cases.Add("\t{");
            cases.Add("\t\tAggregateException e = task.Exception;");
            cases.Add("\t\tif (e != null)");
            cases.Add("\t\t\tcallback(Error(e.GetBaseException()), null);");
            cases.Add("\t\telse");
            cases.Add(string.Format("\t\t\tcallback(null, {0});",
                                    call.ResponseType.HasValue ? string.Format("new {0}.Value {{{1} = task.Result}}", name, call.Name) : "null"));
            cases.Add("\t});");
            cases.Add("}");
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
}

