﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal enum TokenType
    {
        LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
        COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR,

        BANG, BANG_EQUAL,EQUAL, EQUAL_EQUAL,
        GREATER, GREATER_EQUAL, LESS, LESS_EQUAL,

        IDENTIFIER, STRING, NUMBER,

        AND,CLASS,ELES,FALSE,FUN,FOR,IF,NIL,OR,PRINT,RETURN,SUPER,THIS,TRUE,VAR,WHILE,

        EOF
    }
}
