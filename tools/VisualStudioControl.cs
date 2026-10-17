using System;
using System.Text;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using EnvDTE90;
public static class VrVsControl
{
 public static string Run(string action, int pid, string solution)
 {
  var dte=(DTE)Marshal.GetActiveObject("VisualStudio.DTE.17.0");
  var dbg=(Debugger3)dte.Debugger;
  var result=new StringBuilder();
  if(action=="attach")
  {
   if(!string.IsNullOrEmpty(solution)) dte.Solution.Open(solution);
   foreach(ExceptionSettings group in dbg.ExceptionGroups)
    if(group.SupportsExceptionCodes)
     try{var error=group.ItemFromCode(0xc0000005);group.SetBreakWhenThrown(true,error);result.AppendLine("Break on AV: "+group.Name);}catch{}
   foreach(Process2 process in dbg.LocalProcesses)
    if(process.ProcessID==pid)
    {
     process.Attach2(new string[]{"{3B476D35-A401-11D2-AAD4-00C04F990171}","{2E36F1D4-B23C-435D-AB41-18E608940038}"});
     result.AppendLine("Attached native + .NET Core: "+pid);
     return result.ToString();
    }
   throw new InvalidOperationException("Target process not found: "+pid);
  }
  if(action=="symbols"){return "Symbol path: "+dbg.SymbolPath;}
  if(action=="break") dbg.Break(true);
  if(action=="continue"){dbg.Go(false);return "continued";}
  if(action=="detach"){dbg.DetachAll();return "detached";}
  result.AppendLine("Mode: "+dbg.CurrentMode+"; reason: "+dbg.LastBreakReason);
  if(dbg.CurrentMode==dbgDebugMode.dbgBreakMode)
  {
   result.AppendLine("Thread: "+dbg.CurrentThread.ID);
   int frameCount=0;
   foreach(StackFrame frame in dbg.CurrentThread.StackFrames)
   {
    result.AppendLine("FRAME "+frame.Module+" | "+frame.FunctionName+" | "+frame.Language);
    if(++frameCount>=40)break;
    int count=0;
    foreach(Expression local in frame.Locals)
    {result.AppendLine("  "+local.Name+" = "+local.Value);if(++count>=8)break;}
   }
  }
  return result.ToString();
 }
}
