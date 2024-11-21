using System;

namespace SelectChannel;

public class CaseNotReadyException() : InvalidOperationException("case is not ready (forgot to await the select?)")
{
}