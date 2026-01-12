// MpostPatcher.cs
// 필요: NuGet HarmonyLib
using HarmonyLib;
using System.Diagnostics;
using System.Reflection;

public static class MpostPatcher
{
    static bool _patched;
    static readonly string[] RaiseMethodNames = new[]
    {
        "RaiseCalibrateFinishedEvent",
        "RaiseCalibrateProgressEvent",
        "RaiseCalibrateStartEvent",

        "RaiseCashBoxAttachedEvent",
        "RaiseCashboxCleanlinessEvent",
        "RaiseCashBoxRemovedEvent",

        "RaiseCheatedEvent",
        "RaiseClearAuditEvent",

        "RaiseConnectedEvent",
        "RaiseDisconnectedEvent",

        "RaiseErrorWhileSendingMessageEvent",
        "RaiseEscrowEvent",

        "RaiseFailureClearedEvent",
        "RaiseFailureDetectedEvent",

        "RaiseInvalidCommandEvent",

        "RaiseJamClearedEvent",
        "RaiseJamDetectedEvent",

        "RaiseNoteRetrievedEvent",

        "RaisePauseClearedEvent",
        "RaisePauseDetectedEvent",

        "RaisePowerUpCompleteEvent",
        "RaisePowerUpEvent",
        "RaisePUPEscrowEvent",

        "RaiseRejectedEvent",
        "RaiseReturnedEvent",
        "RaiseStackedEvent",

        "RaiseStackerFullClearedEvent",
        "RaiseStackerFullEvent",
        "RaiseStallClearedEvent",
        "RaiseStallDetectedEvent"
    };

    public static void Apply(Type acceptorType)
    {
        if (_patched || acceptorType == null) return;

        try
        {
            var harmony = new Harmony("com.example.mpost.patch.v1");
            var prefix = typeof(MpostPatcher).GetMethod(nameof(GenericRaisePrefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var name in RaiseMethodNames)
            {
                var mi = FindMethod(acceptorType, name);
                if (mi == null)
                {
                    Trace.WriteLine($"[MpostPatcher] method '{name}' not found on {acceptorType.FullName}");
                    continue;
                }
                harmony.Patch(mi, prefix: new HarmonyMethod(prefix));
                Trace.WriteLine($"[MpostPatcher] patched {acceptorType.FullName}.{mi.Name}");
            }

            _patched = true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine("[MpostPatcher] Apply error: " + ex);
        }
    }

    // Harmony prefix signature: return false -> skip original method
    static bool GenericRaisePrefix(object __instance, MethodBase __originalMethod, object[] __args)
    {
        try
        {
            if (__instance == null || __originalMethod == null) return true;

            var methodName = __originalMethod.Name;
            var eventKey = ExtractEventKeyFromRaise(methodName); // "Stacked", "PowerUp", etc.
            var eventName = "On" + eventKey; // look for OnXxx backing delegate

            // 1) flag 조정 (pre-invoke). 일부 메서드는 post-invoke로 조정해야 할 수 있음(예: RaiseDownloadFinishEvent)
            ApplyFlagAdjustments(__instance, methodName, eventKey, __originalMethod, __args);

            // 2) Build candidate EventArgs from original args (best-effort)
            object eventArg = TryBuildEventArgsFromOriginal(__instance.GetType(), methodName, __args);

            // 3) Invoke all matching events (handle multiple events like OnStacked + OnStackedWithDocInfo)
            SafeInvokeWithArgs(__instance, eventName, eventArg, __args);

            // skip original (we invoked handlers ourselves)
            return false;
        }
        catch (Exception ex)
        {
            Trace.WriteLine("[MpostPatcher] GenericRaisePrefix error: " + ex);
            return true;
        }
    }

    #region Flag adjustments
    // 시그니처를 변경: instance, methodName, eventKey 와 optional originalMethod/__args
    static void ApplyFlagAdjustments(object instance, string methodName, string eventKey, MethodBase originalMethod = null, object[] __args = null)
    {
        if (instance == null) return;
        var t = instance.GetType();

        // clear matching _raiseXxxEvent fields by default
        try
        {
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (f.FieldType != typeof(bool)) continue;
                var fname = f.Name ?? "";
                var ek = eventKey ?? "";
                if (fname.IndexOf("_raise", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    fname.IndexOf(ek, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try { f.SetValue(instance, false); Trace.WriteLine($"[MpostPatcher] Cleared {fname}"); } catch { }
                }
            }
        }
        catch (Exception ex) { Trace.WriteLine("[MpostPatcher] ApplyFlagAdjustments scan error: " + ex); }

        // specific cases taken from your original code
        void S(string n, object v) => TrySetField(t, instance, n, v);

        switch (methodName)
        {
            case "RaiseCalibrateFinishedEvent":
                S("_raiseCalibrateFinishEvent", false);
                break;

            case "RaiseCalibrateProgressEvent":
                S("_raiseCalibrateProgressEvent", false);
                break;

            case "RaiseCalibrateStartEvent":
                break;

            case "RaiseCashBoxAttachedEvent":
                S("_raiseCashBoxAttachedEvent", false);
                break;

            case "RaiseCashBoxRemovedEvent":
                S("_raiseCashBoxRemovedEvent", false);
                break;

            case "RaiseCheatedEvent":
                S("_raiseCheatedEvent", false);
                break;

            case "RaiseClearAuditEvent":
                break;

            case "RaiseConnectedEvent":
                S("_raiseConnectedEvent", false);
                S("_raiseDisconnectedEvent", true);
                break;

            case "RaiseDisconnectedEvent":
                S("_raiseDisconnectedEvent", false);
                S("_raiseFailureDetectedEvent", true);
                S("_raiseJamDetectedEvent", true);
                S("_raiseStackerFullEvent", true);
                S("_raiseEscrowEvent", true);
                S("_raiseCashBoxRemovedEvent", true);
                break;

            case "RaiseErrorWhileSendingMessageEvent":
                break;


            case "RaiseEscrowEvent":
                S("_raiseEscrowEvent", false);
                S("_raisePUPEscrowEvent", false);
                break;

            case "RaiseFailureClearedEvent":
                S("_raiseFailureClearedEvent", false);
                S("_raiseFailureDetectedEvent", true);
                break;

            case "RaiseFailureDetectedEvent":
                S("_raiseFailureClearedEvent", false);
                S("_raiseFailureDetectedEvent", true);
                break;

            case "RaiseInvalidCommandEvent":
                break;

            case "RaiseJamClearedEvent":
                S("_raiseJamClearedEvent", false);
                break;

            case "RaiseJamDetectedEvent":
                S("_raiseJamDetectedEvent", false);
                break;

            case "RaiseNoteRetrievedEvent":
                break;

            case "RaisePauseClearedEvent":
                S("_raisePauseClearedEvent", false);
                break;

            case "RaisePauseDetectedEvent":
                S("_raisePauseDetectedEvent", false);
                break;

            case "RaisePowerUpCompleteEvent":
                S("_raisePowerUpCompleteEvent", false);
                break;

            case "RaisePUPEscrowEvent":
                S("_raisePUPEscrowEvent", false);
                S("_raiseEscrowEvent", false);
                break;

            case "RaiseRejectedEvent":
                S("_raiseRejectedEvent", false);
                break;

            case "RaiseReturnedEvent":
                S("_raiseReturnedEvent", false);
                break;

            case "RaiseStackedEvent":
                S("_raiseStackedEvent", false);
                break;

            case "RaiseStackerFullClearedEvent":
                S("_raiseStackerFullClearedEvent", false);
                break;

            case "RaiseStackerFullEvent":
                S("_raiseStackerFullEvent", false);
                break;

            case "RaiseStallClearedEvent":
                S("_raiseStallClearedEvent", false);
                break;

            case "RaiseStallDetectedEvent":
                S("_raiseStallDetectedEvent", false);
                break;
        }
    }
    #endregion

    #region EventArgs construction
    static object TryBuildEventArgsFromOriginal(Type acceptorType, string methodName, object[] __args)
    {
        try
        {
            if (__args != null && __args.Length > 0)
            {
                if (__args[0] is EventArgs ea) return ea;

                var asm = acceptorType.Assembly;
                var argTypes = __args.Select(a => a?.GetType() ?? typeof(object)).ToArray();

                var candidate = asm.GetTypes()
                                   .Where(t => typeof(EventArgs).IsAssignableFrom(t) && !t.IsAbstract)
                                   .Select(t => new
                                   {
                                       Type = t,
                                       Ctor = t.GetConstructors().FirstOrDefault(ctor =>
                                       {
                                           var ps = ctor.GetParameters();
                                           if (ps.Length != argTypes.Length) return false;
                                           for (int i = 0; i < ps.Length; i++)
                                               if (!ps[i].ParameterType.IsAssignableFrom(argTypes[i])) return false;
                                           return true;
                                       })
                                   })
                                   .FirstOrDefault(x => x.Ctor != null);

                if (candidate != null)
                    return candidate.Ctor.Invoke(__args);

                if (__args.Length == 1)
                {
                    var nameHint = methodName.Replace("Raise", "").Replace("Event", "");
                    var heuristic = asm.GetTypes()
                        .FirstOrDefault(t => typeof(EventArgs).IsAssignableFrom(t)
                                             && t.Name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0
                                             && t.GetConstructors().Any(ctor => ctor.GetParameters().Length == 1));
                    if (heuristic != null)
                    {
                        var ctor = heuristic.GetConstructors().First();
                        return ctor.Invoke(new object[] { __args[0] });
                    }
                }
            }
        }
        catch (Exception ex) { Trace.WriteLine("[MpostPatcher] TryBuildEventArgsFromOriginal error: " + ex); }

        return EventArgs.Empty;
    }
    #endregion

    #region Safe invoke
    static void SafeInvokeWithArgs(object instance, string eventName, object builtEventArg, object[] originalArgs)
    {
        try
        {
            var t = instance.GetType();

            var fi = FindField(t, eventName)
                     ?? FindField(t, "_" + eventName)
                     ?? FindField(t, "m_" + eventName)
                     ?? FindField(t, "<" + eventName + ">k__BackingField");

            MulticastDelegate dlg = null;
            if (fi != null) dlg = fi.GetValue(instance) as MulticastDelegate;

            if (dlg == null)
            {
                var ei = FindEvent(t, eventName);
                if (ei != null)
                {
                    var bf = FindField(t, ei.Name) ?? FindField(t, "<" + ei.Name + ">k__BackingField");
                    if (bf != null) dlg = bf.GetValue(instance) as MulticastDelegate;
                }
            }

            if (dlg == null)
            {
                Trace.WriteLine($"[MpostPatcher] no delegates for '{eventName}' on {t.FullName}");
                return;
            }

            foreach (var d in dlg.GetInvocationList())
            {
                var del = d;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var mi = del.Method;
                        var ps = mi.GetParameters();
                        object[] invokeArgs;

                        if (ps.Length == 0) invokeArgs = new object[0];
                        else if (ps.Length == 1)
                        {
                            if (builtEventArg != null && ps[0].ParameterType.IsAssignableFrom(builtEventArg.GetType()))
                                invokeArgs = new object[] { builtEventArg };
                            else if (ps[0].ParameterType.IsAssignableFrom(instance.GetType()))
                                invokeArgs = new object[] { instance };
                            else invokeArgs = new object[] { builtEventArg };
                        }
                        else
                        {
                            var senderArg = instance;
                            object secondArg = builtEventArg;
                            var expectedType = ps[1].ParameterType;
                            if (expectedType != typeof(EventArgs) && !(builtEventArg?.GetType() != null && expectedType.IsAssignableFrom(builtEventArg.GetType())))
                            {
                                var constructed = TryConstructEventArgsOfType(expectedType, originalArgs);
                                if (constructed != null) secondArg = constructed;
                                else if (expectedType.GetConstructor(Type.EmptyTypes) != null)
                                    secondArg = Activator.CreateInstance(expectedType);
                            }

                            invokeArgs = new object[] { senderArg, secondArg };
                        }

                        del.DynamicInvoke(invokeArgs);
                    }
                    catch (TargetInvocationException tie) { Trace.WriteLine("[MpostPatcher] handler threw: " + tie.InnerException?.Message); }
                    catch (Exception ex) { Trace.WriteLine("[MpostPatcher] handler error: " + ex); }
                });
            }
        }
        catch (Exception ex) { Trace.WriteLine("[MpostPatcher] SafeInvokeWithArgs error: " + ex); }
    }

    static object TryConstructEventArgsOfType(Type expectedType, object[] originalArgs)
    {
        try
        {
            if (expectedType == null) return null;
            if (originalArgs == null || originalArgs.Length == 0)
            {
                var def = expectedType.GetConstructor(Type.EmptyTypes);
                if (def != null) return Activator.CreateInstance(expectedType);
                return null;
            }

            var argTypes = originalArgs.Select(a => a?.GetType() ?? typeof(object)).ToArray();
            foreach (var c in expectedType.GetConstructors())
            {
                var ps = c.GetParameters();
                if (ps.Length != argTypes.Length) continue;
                bool ok = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (!ps[i].ParameterType.IsAssignableFrom(argTypes[i])) { ok = false; break; }
                }
                if (ok) return c.Invoke(originalArgs);
            }
        }
        catch (Exception ex) { Trace.WriteLine("[MpostPatcher] TryConstructEventArgsOfType error: " + ex); }
        return null;
    }
    #endregion

    #region Reflection utils
    static MethodInfo FindMethod(Type t, string name)
    {
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var cands = t.GetMethods(flags).Where(m => m.Name == name).ToArray();
            if (cands.Length == 0) return null;
            if (cands.Length == 1) return cands[0];
            var paramless = cands.FirstOrDefault(m => m.GetParameters().Length == 0);
            if (paramless != null) return paramless;
            return cands.OrderBy(m => m.GetParameters().Length).First();
        }
        catch (Exception ex) { Trace.WriteLine("[MpostPatcher] FindMethod error: " + ex); return null; }
    }

    static FieldInfo FindField(Type t, string name)
    {
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var f = t.GetField(name, flags);
            if (f != null) return f;
            return t.GetFields(flags)
                    .FirstOrDefault(fd => fd.Name.Equals(name, StringComparison.Ordinal)
                                       || fd.Name.Equals("_" + name, StringComparison.Ordinal)
                                       || fd.Name.Equals("m_" + name, StringComparison.Ordinal)
                                       || fd.Name.Equals("<" + name + ">k__BackingField", StringComparison.Ordinal)
                                       || fd.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch (Exception ex) { Trace.WriteLine("[MpostPatcher] FindField error: " + ex); return null; }
    }

    static EventInfo FindEvent(Type t, string name)
    {
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var e = t.GetEvent(name, flags);
            if (e != null) return e;
            return t.GetEvents(flags).FirstOrDefault(ev => ev.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) { Trace.WriteLine("[MpostPatcher] FindEvent error: " + ex); return null; }
    }

    static void TrySetField(Type t, object instance, string fieldName, object value)
    {
        try
        {
            var f = FindField(t, fieldName);
            if (f != null) f.SetValue(instance, value);
        }
        catch { }
    }
    #endregion

    #region small utils
    static string ExtractEventKeyFromRaise(string raiseName)
    {
        if (string.IsNullOrEmpty(raiseName)) return raiseName;
        var k = raiseName;
        if (k.StartsWith("Raise")) k = k.Substring(5);
        if (k.EndsWith("Event")) k = k.Substring(0, k.Length - 5);
        return k;
    }

    static bool GetBoolFromArgs(MethodBase __originalMethod, object[] __args)
    {
        if (__args != null && __args.Length > 0 && __args[0] is bool b) return b;
        return false;
    }
    #endregion
}
