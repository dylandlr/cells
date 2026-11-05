using System;
using System.IO;
using System.Text.Json;
using Cells.Experiments;

// Controller transport only: the existing game remains the source of dynamics and utility.
internal static class StrategicSession
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
    { IncludeFields = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static void Emit(TextWriter output, object value)
    {
        output.WriteLine(JsonSerializer.Serialize(value, Json));
        output.Flush();
    }

    internal static void Run(int seed, bool delivery, TextReader input, TextWriter output)
    {
        var game = new SignalingGameV1(seed, SignalingGameV1.DefaultHorizon, delivery);
        // These events are controller/audit records, not shared agent context.
        Emit(output, new { type = "session", protocol = "cells-strategic-v1", seed,
            delivery, horizon = SignalingGameV1.DefaultHorizon });
        game.OnDecisionStep = (step, observations, actions, rewards) =>
            Emit(output, new { type = "transition", step, observations, actions, rewards });
        while (!game.IsFinished)
        {
            var observations = new SignalingGameV1.Observation[SignalingGameV1.Players];
            for (int seat = 0; seat < observations.Length; seat++) observations[seat] = game.Observe(seat);
            Emit(output, new { type = "decision", step = game.StepCount, observations });
            SignalingGameV1.Action[] actions = null;
            while (actions == null)
            {
                string line = input.ReadLine();
                if (line == null)
                {
                    Emit(output, new { type = "aborted", step = game.StepCount, reason = "input_closed" });
                    return; // Never manufacture actions or a completed episode.
                }
                try { actions = Parse(line, game.StepCount); }
                catch (Exception ex) when (ex is JsonException || ex is ArgumentException ||
                    ex is InvalidOperationException || ex is FormatException || ex is OverflowException)
                {
                    Emit(output, new { type = "error", step = game.StepCount, reason = "invalid_batch" });
                }
            }
            game.Advance(actions);
        }
        Emit(output, new { type = "summary", result = game.Summary() });
    }

    private static SignalingGameV1.Action[] Parse(string line, int step)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        RequireFields(root, "step", "actions");
        if (root.GetProperty("step").GetInt32() != step) throw new ArgumentException("Stale round.");
        var entries = root.GetProperty("actions");
        if (entries.GetArrayLength() != SignalingGameV1.Players) throw new ArgumentException("Four actions required.");
        var seen = new bool[SignalingGameV1.Players];
        var actions = new SignalingGameV1.Action[SignalingGameV1.Players];
        foreach (var entry in entries.EnumerateArray())
        {
            RequireFields(entry, "agent", "code");
            int seat = entry.GetProperty("agent").GetInt32();
            int code = entry.GetProperty("code").GetInt32();
            if (seat < 0 || seat >= seen.Length || seen[seat] || code < 0 || code >= SignalingGameV1.ActionCount)
                throw new ArgumentException("Invalid action or seat.");
            seen[seat] = true;
            actions[seat] = new SignalingGameV1.Action(code);
        }
        return actions;
    }

    private static void RequireFields(JsonElement value, params string[] names)
    {
        int count = 0;
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var property in value.EnumerateObject())
        {
            if (Array.IndexOf(names, property.Name) < 0 || !seen.Add(property.Name))
                throw new ArgumentException("Unknown or duplicate field.");
            count++;
        }
        if (count != names.Length) throw new ArgumentException("Missing field.");
    }
}
