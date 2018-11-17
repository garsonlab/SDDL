using SDDL;
using System.Collections.Generic;
using System.IO;

public class CSharpAsync : CSharp
{
    private class TypedefWriterAsync : TypedefWriter
    {
        public TypedefWriterAsync(string name)
            : base(name)
        {
        }

        public override void WriteInit(StreamWriter writer, string indent, bool withvalue)
        {
            if (withvalue)
            {
                writer.WriteLine(indent + "public TValue Result { get; private set; }");
            }
            base.WriteInit(writer, indent, withvalue);
        }

        public override void WriteOnce(StreamWriter writer, string indent, bool withvalue)
        {
            writer.WriteLine(indent + "public Asyncable Once()");
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "\treturn Asyncable.Create(callback => Self.ListenOnce(Place, value =>");
            writer.WriteLine(indent + "\t{");
            if (withvalue)
            {
                writer.WriteLine(indent + "\t\tif (value == null || !Verify(value.Value))");
                writer.WriteLine(indent + "\t\t\treturn false;");
                writer.WriteLine(indent + "\t\tResult = Output(value.Value);");
                writer.WriteLine(indent + "\t\tcallback();");
            }
            else
            {
                writer.WriteLine(indent + "\t\tcallback();");
            }
            writer.WriteLine(indent + "\t\treturn true;");
            writer.WriteLine(indent + "\t}));");
            writer.WriteLine(indent + "}");
        }
    }

    public CSharpAsync(string ns)
        : base(ns)
    {
    }

    public override void Prepare(StreamWriter writer)
    {
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using SDDL;");
        if (base.ns != "Game")
        {
            writer.WriteLine("using Module;");
        }
        writer.WriteLine();
        if (!string.IsNullOrEmpty(base.ns))
        {
            writer.WriteLine("namespace {0}", base.ns);
            writer.WriteLine("{");
        }
    }

    public override TypedefWriter CreateTypedefWriter(string name)
    {
        return new TypedefWriterAsync(name);
    }

    private string RequestWrap(bool request, bool response)
    {
        return string.Format("{0}Wrap", (request ? "Req" : "") + (response ? "Res" : ""));
    }

    private void RequestWrap(StreamWriter writer, string indent, string name, bool request, bool response)
    {
        string text = this.RequestWrap(request, response);
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic class {0}<T{1}{2}> where T : Output", text, request ? ", TRequest" : "", response ? ", TResponse" : "");
        writer.WriteLine(indent + "\t{");
        writer.WriteLine(indent + "\t\tprivate readonly {0}<T> Self;", name);
        writer.WriteLine(indent + "\t\tprivate readonly int Place;");
        if (request)
        {
            writer.WriteLine(indent + "\t\tprivate readonly Func<TRequest, {0}.Value> Input;", name);
        }
        if (response)
        {
            writer.WriteLine(indent + "\t\tprivate readonly Func<Response.{0}.Value, bool> Verify;", name);
            writer.WriteLine(indent + "\t\tprivate readonly Func<Response.{0}.Value, TResponse> Output;", name);
        }
        writer.WriteLine(indent + "\t\tpublic {0}({1}<T> self, int place{2}{3})", text, name, request ? string.Format(", Func<TRequest, {0}.Value> input", name) : "", response ? string.Format(", Func<Response.{0}.Value, bool> verify, Func<Response.{0}.Value, TResponse> output", name) : "");
        writer.WriteLine(indent + "\t\t{");
        writer.WriteLine(indent + "\t\t\tSelf = self;");
        writer.WriteLine(indent + "\t\t\tPlace = place;");
        if (request)
        {
            writer.WriteLine(indent + "\t\t\tInput = input;");
        }
        if (response)
        {
            writer.WriteLine(indent + "\t\t\tVerify = verify;");
            writer.WriteLine(indent + "\t\t\tOutput = output;");
        }
        writer.WriteLine(indent + "\t\t}");
        if (response)
        {
            writer.WriteLine(indent + "\t\tpublic TResponse Result { get; private set; }");
        }
        writer.WriteLine(indent + "\t\tpublic Asyncable Call({0})", request ? "TRequest request" : "");
        writer.WriteLine(indent + "\t\t{");
        writer.WriteLine(indent + "\t\t\treturn Asyncable.Create(callback => Self.Call(Place, {0}, (error, response) =>", request ? "Input(request)" : "null");
        writer.WriteLine(indent + "\t\t\t{");
        if (response)
        {
            writer.WriteLine(indent + "\t\t\t\tif (error == null)");
            writer.WriteLine(indent + "\t\t\t\t{");
            writer.WriteLine(indent + "\t\t\t\t\tif (response == null || !Verify(response.Value))");
            writer.WriteLine(indent + "\t\t\t\t\t\treturn false;");
            writer.WriteLine(indent + "\t\t\t\t}");
            writer.WriteLine(indent + "\t\t\t\telse");
            writer.WriteLine(indent + "\t\t\t\t{");
            writer.WriteLine(indent + "\t\t\t\t\tthrow new Exception(error);");
            writer.WriteLine(indent + "\t\t\t\t}");
            writer.WriteLine(indent + "\t\t\t\tResult = Output(response.Value);");
            writer.WriteLine(indent + "\t\t\t\tcallback();");
        }
        else
        {
            writer.WriteLine(indent + "\t\t\t\tif (error != null)");
            writer.WriteLine(indent + "\t\t\t\t\tthrow new Exception(error);");
            writer.WriteLine(indent + "\t\t\t\tcallback();");
        }
        writer.WriteLine(indent + "\t\t\t\treturn true;");
        writer.WriteLine(indent + "\t\t\t}));");
        writer.WriteLine(indent + "\t\t}");
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
    }

    public override void Request(StreamWriter writer, string indent, string name, params Call[] calls)
    {
        writer.WriteLine(indent + "public abstract class {0}<T> : RPC.Request<{0}.Value, Response.{0}.Value, T> where T : Output", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tprotected {0}(Func<Protocol.Format> acquire, Action<Protocol.Format> release) : base(acquire, release)", name);
        writer.WriteLine(indent + "\t{");
        List<string> list = new List<string>();
        List<string> list2 = new List<string>();
        for (int i = 0; i < calls.Length; i++)
        {
            Call call = calls[i];
            list.Add("T");
            if (call.RequestType.HasValue)
            {
                list.Add(CSharp.GetType(call.RequestType.Value, call.RequestTypeEx));
            }
            if (call.ResponseType.HasValue)
            {
                list.Add(CSharp.GetType(call.ResponseType.Value, call.ResponseTypeEx));
            }
            string text = string.Format("{0}.{1}{2}", name, this.RequestWrap(call.RequestType.HasValue, call.ResponseType.HasValue), (list.Count == 0) ? "" : string.Format("<{0}>", string.Join(", ", list)));
            list.Clear();
            list.Add(call.Place.ToString());
            if (call.RequestType.HasValue)
            {
                list.Add(string.Format("{0}.InputWrap.{1}", name, call.Name));
            }
            if (call.ResponseType.HasValue)
            {
                list.Add(string.Format("{0}.VerifyWrap.{1}", name, call.Name));
                list.Add(string.Format("{0}.OutputWrap.{1}", name, call.Name));
            }
            writer.WriteLine(indent + "\t\t{0} = new {1}(this, {2});", call.Name, text, string.Join(", ", list));
            list.Clear();
            list2.Add(string.Format("public readonly {0} {1};", text, call.Name));
        }
        writer.WriteLine(indent + "\t}");
        foreach (string item in list2)
        {
            writer.WriteLine(indent + "\t" + item);
        }
        writer.WriteLine(indent + "}");
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic static class InputWrap");
        writer.WriteLine(indent + "\t{");
        for (int j = 0; j < calls.Length; j++)
        {
            Call call2 = calls[j];
            if (call2.RequestType.HasValue)
            {
                writer.WriteLine(indent + "\t\tpublic static readonly Func<{0}, {1}.Value> {2} = request => new {1}.Value {{{2} = request}};", CSharp.GetType(call2.RequestType.Value, call2.RequestTypeEx), name, call2.Name);
            }
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic static class VerifyWrap");
        writer.WriteLine(indent + "\t{");
        for (int k = 0; k < calls.Length; k++)
        {
            Call call3 = calls[k];
            if (call3.ResponseType.HasValue)
            {
                writer.WriteLine(indent + "\t\tpublic static readonly Func<Response.{0}.Value, bool> {1} = response => response.{1} != null;", name, call3.Name);
            }
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
        writer.WriteLine(indent + "public static partial class {0}", name);
        writer.WriteLine(indent + "{");
        writer.WriteLine(indent + "\tpublic static class OutputWrap");
        writer.WriteLine(indent + "\t{");
        for (int l = 0; l < calls.Length; l++)
        {
            Call call4 = calls[l];
            if (call4.ResponseType.HasValue)
            {
                writer.WriteLine(indent + "\t\tpublic static readonly Func<Response.{0}.Value, {1}> {2} = response => response.{2}{3};", name, CSharp.GetType(call4.ResponseType.Value, call4.ResponseTypeEx), call4.Name, (call4.ResponseType.Value == Type.String || call4.ResponseType.Value == Type.Other) ? "" : ".Value");
            }
        }
        writer.WriteLine(indent + "\t}");
        writer.WriteLine(indent + "}");
        this.RequestWrap(writer, indent, name, true, true);
        this.RequestWrap(writer, indent, name, true, false);
        this.RequestWrap(writer, indent, name, false, true);
        this.RequestWrap(writer, indent, name, false, false);
    }
}
