﻿/*
 *  Copyright © 2016 Thomas R. Lawrence
 * 
 *  GNU Lesser General Public License
 * 
 *  This file is part of TreeLib
 * 
 *  TreeLib is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program. If not, see <http://www.gnu.org/licenses/>.
 * 
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.FindSymbols;

namespace BuildTool
{
    public class SyntaxNodeReplacementRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<SyntaxNode, SyntaxNode> replacements;

        public SyntaxNodeReplacementRewriter(Dictionary<SyntaxNode, SyntaxNode> replacements)
        {
            this.replacements = replacements;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node != null)
            {
                SyntaxNode replacement;
                if (replacements.TryGetValue(node, out replacement))
                {
                    return replacement;
                }
            }
            return base.Visit(node);
        }
    }
}
