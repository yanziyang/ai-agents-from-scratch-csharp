using AiAgents.Core.Client;
using OpenAI.Chat;

Console.WriteLine("Translation started...\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

const string systemPrompt = """
    Du bist ein erfahrener wissenschaftlicher Übersetzer für technische Texte aus dem Englischen ins Deutsche.

    Deine Aufgabe: Erstelle eine inhaltlich exakte Übersetzung, die den vollen Sinn und die technische Präzision
    des Originaltexts erhält.

    Gleichzeitig soll die Übersetzung klar, natürlich und leicht lesbar auf Deutsch klingen – also so, wie ein
    deutscher Wissenschaftler oder Ingenieur denselben Text schreiben würde.

    Befolge diese Regeln:
    Bewahre jede fachliche Aussage und Nuance exakt. Kein Inhalt darf verloren gehen oder verändert werden.
    Verwende idiomatisches, flüssiges Deutsch, wie es in wissenschaftlichen Abstracts (z. B. NeurIPS, ICLR, AAAI) üblich ist.
    Vermeide wörtliche Satzstrukturen. Formuliere so, wie ein deutscher Wissenschaftler denselben Inhalt selbst schreiben würde.
    Verwende korrekte Terminologie (z. B. Multi-Agenten-System, Adapterlayer, Baseline, Strategieverbesserung).
    Verwende bei Zahlen, Einheiten und Prozentangaben deutsche Typografie (z. B. „54 %“, „3 m“, „2 000“).
    Passe zusammengesetzte Begriffe an die deutsche Grammatik an (z. B. „kontinuierlich lernendes System“ statt „kontinuierliches Lernen System“).
    Kürze lange oder verschachtelte Sätze behutsam, ohne Bedeutung zu verändern, um Lesbarkeit zu verbessern.
    Verwende einen neutralen, wissenschaftlichen Stil, ohne Werbesprache oder unnötige Ausschmückung.

    Zusatzinstruktion:
    Wenn der Originaltext englische Satzlogik enthält, restrukturiere den Satz so, dass er auf Deutsch elegant und klar klingt, aber denselben Inhalt vermittelt.

    Zielqualität: Eine Übersetzung, die sich wie ein Originaltext liest – technisch präzise, flüssig und grammatikalisch einwandfrei.

    DO NOT add any additional text or explanation. ONLY respond with the translated text.
    """;

const string userPrompt = """
    Translate this text into German:

    We address the long-horizon gap in large language model (LLM) agents by enabling them to sustain coherent strategies in adversarial, stochastic environments.
    Settlers of Catan provides a challenging benchmark: success depends on balancing short- and long-term goals amid randomness, trading, expansion, and blocking.
    Prompt-centric LLM agents (e.g., ReAct, Reflexion) must re-interpret large, evolving game states each turn, quickly saturating context windows and losing strategic consistency.
    We propose HexMachina, a continual learning multi-agent system that separates environment discovery (inducing an adapter layer without documentation) from strategy improvement (evolving a compiled player through code refinement and simulation).
    This design preserves executable artifacts, allowing the LLM to focus on high-level strategy rather than per-turn reasoning.
    In controlled Catanatron experiments, HexMachina learns from scratch and evolves players that outperform the strongest human-crafted baseline (AlphaBeta), achieving a 54% win rate and surpassing prompt-driven and no-discovery baselines.
    Ablations confirm that isolating pure strategy learning improves performance.
    Overall, artifact-centric continual learning transforms LLMs from brittle stepwise deciders into stable strategy designers, advancing long-horizon autonomy.
    """;

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage(systemPrompt),
    ChatMessage.CreateUserMessage(userPrompt)
};

var response = await chatClient.CompleteChatAsync(messages);
Console.WriteLine("AI: " + response.Value.Content[0].Text);
