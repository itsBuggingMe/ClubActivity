using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClubActivity;

public abstract class LangBase
{
    public string Extension { get; init; }
    public string Language { get; init; }
    public LangBase(string extension, string language)
    {
        Extension = extension;
        Language = language;
    }

    public abstract IFunction? Compile(string source);
}
