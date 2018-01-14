﻿// Generated by TinyPG v1.3 available at www.codeproject.com

namespace Spark.Direct3D11.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml.Serialization;

    using Spark.Graphics;

    #region ParseTree
    [Serializable]
    internal class ParseErrors : List<ParseError>
    {
    }

    [Serializable]
    internal class ParseError
    {
        private string message;
        private int code;
        private int line;
        private int col;
        private int pos;
        private int length;

        public int Code { get { return code; } }
        public int Line { get { return line; } }
        public int Column { get { return col; } }
        public int Position { get { return pos; } }
        public int Length { get { return length; } }
        public string Message { get { return message; } }

        // just for the sake of serialization
        public ParseError()
        {
        }

        public ParseError(string message, int code, ParseNode node) : this(message, code, 0, node.Token.StartPos, node.Token.StartPos, node.Token.Length)
        {
        }

        public ParseError(string message, int code, int line, int col, int pos, int length)
        {
            this.message = message;
            this.code = code;
            this.line = line;
            this.col = col;
            this.pos = pos;
            this.length = length;
        }
    }

    // rootlevel of the node tree
    [Serializable]
    internal partial class ParseTree : ParseNode
    {
        public ParseErrors Errors;

        public List<Token> Skipped;

        public ParseTree() : base(new Token(), "ParseTree")
        {
            Token.Type = TokenType.Start;
            Token.Text = "Root";
            Errors = new ParseErrors();
        }

        public string PrintTree()
        {
            StringBuilder sb = new StringBuilder();
            int indent = 0;
            PrintNode(sb, this, indent);
            return sb.ToString();
        }

        private void PrintNode(StringBuilder sb, ParseNode node, int indent)
        {

            string space = "".PadLeft(indent, ' ');

            sb.Append(space);
            sb.AppendLine(node.Text);

            foreach (ParseNode n in node.Nodes)
                PrintNode(sb, n, indent + 2);
        }

        /// <summary>
        /// this is the entry point for executing and evaluating the parse tree.
        /// </summary>
        /// <param name="paramlist">additional optional input parameters</param>
        /// <returns>the output of the evaluation function</returns>
        public object Eval(params object[] paramlist)
        {
            return Nodes[0].Eval(this, paramlist);
        }
    }

    [Serializable]
    [XmlInclude(typeof(ParseTree))]
    internal partial class ParseNode
    {
        protected string text;
        protected List<ParseNode> nodes;

        public List<ParseNode> Nodes { get { return nodes; } }

        [XmlIgnore] // avoid circular references when serializing
        public ParseNode Parent;
        public Token Token; // the token/rule

        [XmlIgnore] // skip redundant text (is part of Token)
        public string Text
        { // text to display in parse tree 
            get { return text; }
            set { text = value; }
        }

        public virtual ParseNode CreateNode(Token token, string text)
        {
            ParseNode node = new ParseNode(token, text);
            node.Parent = this;
            return node;
        }

        protected ParseNode(Token token, string text)
        {
            this.Token = token;
            this.text = text;
            this.nodes = new List<ParseNode>();
        }

        protected object GetValue(ParseTree tree, TokenType type, int index)
        {
            return GetValue(tree, type, ref index);
        }

        protected object GetValue(ParseTree tree, TokenType type, ref int index)
        {
            object o = null;
            if (index < 0) return o;

            // left to right
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == type)
                {
                    index--;
                    if (index < 0)
                    {
                        o = node.Eval(tree);
                        break;
                    }
                }
            }
            return o;
        }

        /// <summary>
        /// this implements the evaluation functionality, cannot be used directly
        /// </summary>
        /// <param name="tree">the parsetree itself</param>
        /// <param name="paramlist">optional input parameters</param>
        /// <returns>a partial result of the evaluation</returns>
        internal object Eval(ParseTree tree, params object[] paramlist)
        {
            object Value = null;

            switch (Token.Type)
            {
                case TokenType.Start:
                    Value = EvalStart(tree, paramlist);
                    break;
                case TokenType.Technique_Declaration:
                    Value = EvalTechnique_Declaration(tree, paramlist);
                    break;
                case TokenType.Pass_Declaration:
                    Value = EvalPass_Declaration(tree, paramlist);
                    break;
                case TokenType.SetVertexShader_Expression:
                    Value = EvalSetVertexShader_Expression(tree, paramlist);
                    break;
                case TokenType.SetPixelShader_Expression:
                    Value = EvalSetPixelShader_Expression(tree, paramlist);
                    break;
                case TokenType.SetGeometryShader_Expression:
                    Value = EvalSetGeometryShader_Expression(tree, paramlist);
                    break;
                case TokenType.SetDomainShader_Expression:
                    Value = EvalSetDomainShader_Expression(tree, paramlist);
                    break;
                case TokenType.SetHullShader_Expression:
                    Value = EvalSetHullShader_Expression(tree, paramlist);
                    break;
                case TokenType.SetComputeShader_Expression:
                    Value = EvalSetComputeShader_Expression(tree, paramlist);
                    break;

                default:
                    Value = Token.Text;
                    break;
            }
            return Value;
        }

        protected virtual object EvalStart(ParseTree tree, params object[] paramlist)
        {
            EffectContent effect = new EffectContent();
            foreach (ParseNode node in Nodes)
            {
                node.Eval(tree, effect);
            }

            return effect;
        }

        protected virtual object EvalTechnique_Declaration(ParseTree tree, params object[] paramlist)
        {
            // Treat the technique has a list of shader groups, each shader group will have the name of {TechName}-{PassName} in order to allow us to parse legacy FX files.
            // Essentially this will mean that the the techniques/passes get flattened into one list
            List<EffectContent.ShaderGroupContent> technique = new List<EffectContent.ShaderGroupContent>();
            string techName = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;

            foreach (ParseNode node in Nodes)
            {
                EffectContent.ShaderGroupContent pass = node.Eval(tree, technique) as EffectContent.ShaderGroupContent;
                if (pass != null)
                {
                    technique.Add(pass);
                }
            }

            // Do a second pass over the passes and do any renaming
            bool hasMoreThanOnePass = technique.Count > 1;
            int currentPassIndex = 0;

            bool doesTechNameExist = !String.IsNullOrEmpty(techName);
            EffectContent effect = paramlist[0] as EffectContent;

            foreach (EffectContent.ShaderGroupContent grp in technique)
            {
                bool doesPassNameExist = !String.IsNullOrEmpty(grp.Name);

                // Use {TechName}-{PassName} scheme if both are named
                if (doesTechNameExist && doesPassNameExist)
                {
                    grp.Name = techName + "-" + grp.Name;
                }
                else if (doesTechNameExist && !doesPassNameExist)
                {
                    // If tech name does exist and no pass name, then use the technique name, unless if we have multiple passes, then assign a name based on index
                    if (hasMoreThanOnePass)
                    {
                        grp.Name = techName + "-" + currentPassIndex.ToString();
                    }
                    else
                    {
                        grp.Name = techName;
                    }
                }
                else if (!doesTechNameExist && !doesPassNameExist)
                {
                    // If tech name does not exist and neither does the pass, set the index as the name
                    grp.Name = currentPassIndex.ToString();
                }
                // Else just use whatever the pass name is coming in

                currentPassIndex++;

                effect.AddShaderGroup(grp);
            }

            return null;
        }

        protected virtual object EvalPass_Declaration(ParseTree tree, params object[] paramlist)
        {
            // Each pass is a ShaderGroup technically, but we concat its name with the technique's name
            EffectContent.ShaderGroupContent pass = new EffectContent.ShaderGroupContent();
            pass.Name = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;

            foreach (ParseNode node in Nodes)
            {
                node.Eval(tree, pass);
            }

            return pass;
        }

        protected virtual object EvalSetVertexShader_Expression(ParseTree tree, params object[] paramlist)
        {
            if (string.IsNullOrEmpty(this.GetValue(tree, TokenType.Identifier, 0) as string) || string.IsNullOrEmpty(this.GetValue(tree, TokenType.VSShaderProfile, 0) as string))
            {
                return null;
            }

            EffectContent.ShaderContent shader = new EffectContent.ShaderContent();
            shader.ShaderType = ShaderStage.VertexShader;
            shader.EntryPoint = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;
            shader.ShaderProfile = this.GetValue(tree, TokenType.VSShaderProfile, 0) as string ?? string.Empty;

            // Can be either legacy Pass or new ShaderGroup syntax
            EffectContent.ShaderGroupContent grp = paramlist[0] as EffectContent.ShaderGroupContent;
            if (grp != null)
            {
                grp[ShaderStage.VertexShader] = shader;
            }

            return null;
        }

        protected virtual object EvalSetPixelShader_Expression(ParseTree tree, params object[] paramlist)
        {
            if (string.IsNullOrEmpty(this.GetValue(tree, TokenType.Identifier, 0) as string) || string.IsNullOrEmpty(this.GetValue(tree, TokenType.PSShaderProfile, 0) as string))
                return null;

            EffectContent.ShaderContent shader = new EffectContent.ShaderContent();
            shader.ShaderType = ShaderStage.PixelShader;
            shader.EntryPoint = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;
            shader.ShaderProfile = this.GetValue(tree, TokenType.PSShaderProfile, 0) as string ?? string.Empty;

            // Can be either legacy Pass or new ShaderGroup syntax
            EffectContent.ShaderGroupContent grp = paramlist[0] as EffectContent.ShaderGroupContent;
            if (grp != null)
            {
                grp[ShaderStage.PixelShader] = shader;
            }

            return null;
        }

        protected virtual object EvalSetGeometryShader_Expression(ParseTree tree, params object[] paramlist)
        {
            if (string.IsNullOrEmpty(this.GetValue(tree, TokenType.Identifier, 0) as string) || string.IsNullOrEmpty(this.GetValue(tree, TokenType.GSShaderProfile, 0) as string))
                return null;

            EffectContent.ShaderContent shader = new EffectContent.ShaderContent();
            shader.ShaderType = ShaderStage.GeometryShader;
            shader.EntryPoint = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;
            shader.ShaderProfile = this.GetValue(tree, TokenType.GSShaderProfile, 0) as string ?? string.Empty;

            // Can be either legacy Pass or new ShaderGroup syntax
            EffectContent.ShaderGroupContent grp = paramlist[0] as EffectContent.ShaderGroupContent;
            if (grp != null)
            {
                grp[ShaderStage.GeometryShader] = shader;
            }

            return null;
        }

        protected virtual object EvalSetDomainShader_Expression(ParseTree tree, params object[] paramlist)
        {
            if (string.IsNullOrEmpty(this.GetValue(tree, TokenType.Identifier, 0) as string) || string.IsNullOrEmpty(this.GetValue(tree, TokenType.DSShaderProfile, 0) as string))
                return null;

            EffectContent.ShaderContent shader = new EffectContent.ShaderContent();
            shader.ShaderType = ShaderStage.DomainShader;
            shader.EntryPoint = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;
            shader.ShaderProfile = this.GetValue(tree, TokenType.DSShaderProfile, 0) as string ?? string.Empty;

            // Can be either legacy Pass or new ShaderGroup syntax
            EffectContent.ShaderGroupContent grp = paramlist[0] as EffectContent.ShaderGroupContent;
            if (grp != null)
            {
                grp[ShaderStage.DomainShader] = shader;
            }

            return null;
        }

        protected virtual object EvalSetHullShader_Expression(ParseTree tree, params object[] paramlist)
        {
            if (string.IsNullOrEmpty(this.GetValue(tree, TokenType.Identifier, 0) as string) || string.IsNullOrEmpty(this.GetValue(tree, TokenType.HSShaderProfile, 0) as string))
            {
                return null;
            }

            EffectContent.ShaderContent shader = new EffectContent.ShaderContent();
            shader.ShaderType = ShaderStage.HullShader;
            shader.EntryPoint = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;
            shader.ShaderProfile = this.GetValue(tree, TokenType.HSShaderProfile, 0) as string ?? string.Empty;

            // Can be either legacy Pass or new ShaderGroup syntax
            EffectContent.ShaderGroupContent grp = paramlist[0] as EffectContent.ShaderGroupContent;
            if (grp != null)
            {
                grp[ShaderStage.HullShader] = shader;
            }

            return null;
        }

        protected virtual object EvalSetComputeShader_Expression(ParseTree tree, params object[] paramlist)
        {
            if (string.IsNullOrEmpty(this.GetValue(tree, TokenType.Identifier, 0) as string) || string.IsNullOrEmpty(this.GetValue(tree, TokenType.CSShaderProfile, 0) as string))
            {
                return null;
            }

            EffectContent.ShaderContent shader = new EffectContent.ShaderContent();
            shader.ShaderType = ShaderStage.ComputeShader;
            shader.EntryPoint = this.GetValue(tree, TokenType.Identifier, 0) as string ?? string.Empty;
            shader.ShaderProfile = this.GetValue(tree, TokenType.CSShaderProfile, 0) as string ?? string.Empty;

            // Can be either legacy Pass or new ShaderGroup syntax
            EffectContent.ShaderGroupContent grp = paramlist[0] as EffectContent.ShaderGroupContent;
            if (grp != null)
            {
                grp[ShaderStage.ComputeShader] = shader;
            }

            return null;
        }


    }

    #endregion ParseTree
}
