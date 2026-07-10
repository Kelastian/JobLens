using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using JobLens.Domain.Enums;
using JobLens.Domain.Models;

namespace JobLens.Application.Prompts;

/// <summary>
/// Builds the prompts sent to an ILlmProvider. Every prompt is a C# method,
/// not a scattered string literal — see Prompts.notas.md for why. Branches
/// by output language only; the input (detected) language travels as data
/// inside the prompt text rather than as a second axis of methods.
/// </summary>
public sealed class PromptBuilder
{
    // System.Text.Json escapes all non-ASCII characters (á, 日本語, etc.) to
    // \uXXXX by default. That would turn Japanese requirements/profile data
    // into unreadable escape sequences inside the prompt, so every Unicode
    // range is explicitly allowed through as literal text. See Prompts.notas.md.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public string BuildExtractionPrompt(JobPosting posting, Language outputLanguage)
    {
        return outputLanguage switch
        {
            Language.Es => BuildExtractionPromptEs(posting),
            Language.En => BuildExtractionPromptEn(posting),
            Language.Ja => BuildExtractionPromptJa(posting),
            _ => throw new ArgumentOutOfRangeException(nameof(outputLanguage), outputLanguage, null)
        };
    }

    public string BuildMatchPrompt(ExtractedRequirements requirements, UserProfile profile, Language outputLanguage)
    {
        var requirementsJson = JsonSerializer.Serialize(requirements, SerializerOptions);
        var profileJson = JsonSerializer.Serialize(profile, SerializerOptions);

        return outputLanguage switch
        {
            Language.Es => BuildMatchPromptEs(requirementsJson, profileJson),
            Language.En => BuildMatchPromptEn(requirementsJson, profileJson),
            Language.Ja => BuildMatchPromptJa(requirementsJson, profileJson),
            _ => throw new ArgumentOutOfRangeException(nameof(outputLanguage), outputLanguage, null)
        };
    }

    private static string BuildExtractionPromptEs(JobPosting posting)
    {
        return $"""
            Sos un analista experto en el mercado laboral tech de Japón. A continuación
            hay el texto de una oferta de trabajo, detectado en idioma "{posting.DetectedLanguage}".

            Tu tarea: leer la oferta y devolver ÚNICAMENTE un objeto JSON (sin texto
            adicional antes o después, sin bloque de código markdown) con esta forma
            exacta:

            {ExtractedRequirementsJsonSchema}

            Reglas importantes:
            - "summary" debe estar escrito en ESPAÑOL, sin importar el idioma original de la oferta.
            - "techStack" es una lista de tecnologías tal como aparecen en la oferta (podés mantener nombres propios como "React" o "AWS" sin traducir).
            - Los valores de "seniority", "workStyle", "japaneseRequired.level" y "englishRequired.level" deben ser EXACTAMENTE uno de los valores permitidos en inglés listados en el schema — no los traduzcas.
            - "japaneseRequired.rawText" y "englishRequired.rawText" deben conservar la frase textual original de la oferta (en el idioma original), o null si la oferta no menciona ese idioma.
            - Si un dato no aparece en la oferta, usá null (para strings/números) o "NotSpecified" (para los enums), nunca inventes un valor.

            Texto de la oferta:
            \"\"\"
            {posting.RawText}
            \"\"\"
            """;
    }

    private static string BuildExtractionPromptEn(JobPosting posting)
    {
        return $"""
            You are an expert analyst of Japan's tech job market. Below is the text of
            a job posting, detected as being in "{posting.DetectedLanguage}".

            Your task: read the posting and return ONLY a JSON object (no surrounding
            text, no markdown code block) with this exact shape:

            {ExtractedRequirementsJsonSchema}

            Important rules:
            - "summary" must be written in ENGLISH, regardless of the posting's original language.
            - "techStack" is a list of technologies as they appear in the posting (proper nouns like "React" or "AWS" can stay as-is).
            - The values of "seniority", "workStyle", "japaneseRequired.level", and "englishRequired.level" must be EXACTLY one of the allowed English values listed in the schema — do not translate them.
            - "japaneseRequired.rawText" and "englishRequired.rawText" must preserve the original wording from the posting (in its original language), or null if that language isn't mentioned.
            - If a field isn't present in the posting, use null (for strings/numbers) or "NotSpecified" (for enums) — never invent a value.

            Posting text:
            \"\"\"
            {posting.RawText}
            \"\"\"
            """;
    }

    private static string BuildExtractionPromptJa(JobPosting posting)
    {
        return $"""
            あなたは日本のIT求人市場に精通したアナリストです。以下は「{posting.DetectedLanguage}」と
            判定された求人票の本文です。

            タスク：求人票を読み、以下の正確な形式のJSONオブジェクトのみを返してください
            （前後に説明文を付けず、Markdownのコードブロックも使わないこと）。

            {ExtractedRequirementsJsonSchema}

            重要なルール：
            - "summary" は、求人票の元の言語に関わらず、必ず日本語で記述してください。
            - "techStack" は求人票に記載された技術名のリストです（"React" や "AWS" のような固有名詞はそのままで構いません）。
            - "seniority"、"workStyle"、"japaneseRequired.level"、"englishRequired.level" の値は、スキーマに記載された英語の許容値のいずれかと完全に一致させてください（翻訳しないこと）。
            - "japaneseRequired.rawText" と "englishRequired.rawText" には、求人票に書かれた元の表現（元の言語のまま）を保持してください。該当する記載がなければ null にしてください。
            - 求人票に記載がない項目は、null（文字列・数値の場合）または "NotSpecified"（enumの場合）としてください。情報を推測して補わないでください。

            求人票の本文：
            \"\"\"
            {posting.RawText}
            \"\"\"
            """;
    }

    private static string BuildMatchPromptEs(string requirementsJson, string profileJson)
    {
        return $"""
            Sos un asesor de carrera experto en el mercado laboral tech de Japón. A
            continuación hay los requisitos extraídos de una oferta de trabajo y el
            perfil de un candidato. Tu tarea es evaluar qué tan bien encaja el
            candidato con la oferta.

            Devolvé ÚNICAMENTE un objeto JSON (sin texto adicional, sin bloque de
            código markdown) con esta forma exacta:

            {MatchResultJsonSchema}

            Reglas importantes:
            - "score" es un entero de 0 a 100.
            - "strengths", "gaps" y "suggestions" deben estar escritos en ESPAÑOL.
            - "suggestions" debe tener entre 2 y 3 sugerencias concretas y accionables sobre cómo el candidato podría enmarcar su postulación para esta oferta específica.
            - Basá la evaluación únicamente en los datos provistos abajo, sin inventar información sobre el candidato o la oferta.

            Requisitos de la oferta (JSON):
            {requirementsJson}

            Perfil del candidato (JSON):
            {profileJson}
            """;
    }

    private static string BuildMatchPromptEn(string requirementsJson, string profileJson)
    {
        return $"""
            You are a career advisor specialized in Japan's tech job market. Below are
            the requirements extracted from a job posting and a candidate's profile.
            Your task is to evaluate how well the candidate matches the posting.

            Return ONLY a JSON object (no surrounding text, no markdown code block)
            with this exact shape:

            {MatchResultJsonSchema}

            Important rules:
            - "score" is an integer from 0 to 100.
            - "strengths", "gaps", and "suggestions" must be written in ENGLISH.
            - "suggestions" must contain 2 to 3 concrete, actionable suggestions on how the candidate could frame their application for this specific posting.
            - Base the evaluation only on the data provided below — do not invent information about the candidate or the posting.

            Posting requirements (JSON):
            {requirementsJson}

            Candidate profile (JSON):
            {profileJson}
            """;
    }

    private static string BuildMatchPromptJa(string requirementsJson, string profileJson)
    {
        return $"""
            あなたは日本のIT求人市場に精通したキャリアアドバイザーです。以下は、ある求人票から
            抽出された要件と、候補者のプロフィールです。候補者がこの求人にどれだけ適合するかを
            評価してください。

            以下の正確な形式のJSONオブジェクトのみを返してください
            （前後に説明文を付けず、Markdownのコードブロックも使わないこと）。

            {MatchResultJsonSchema}

            重要なルール：
            - "score" は0から100の整数です。
            - "strengths"、"gaps"、"suggestions" は必ず日本語で記述してください。
            - "suggestions" には、候補者がこの求人に応募する際の応募書類の書き方について、2〜3個の具体的で実行可能な提案を含めてください。
            - 評価は以下に提供されたデータのみに基づいて行い、候補者や求人について情報を推測しないでください。

            求人要件（JSON）：
            {requirementsJson}

            候補者プロフィール（JSON）：
            {profileJson}
            """;
    }

    private const string ExtractedRequirementsJsonSchema = """
        {
          "techStack": string[],
          "seniority": "NotSpecified" | "Intern" | "NewGrad" | "Junior" | "MidLevel" | "Senior" | "Lead",
          "minYearsExperience": number | null,
          "japaneseRequired": {
            "level": "NotSpecified" | "None" | "N5" | "N4" | "N3" | "N2" | "N1",
            "rawText": string | null
          },
          "englishRequired": {
            "level": "NotSpecified" | "None" | "A1" | "A2" | "B1" | "B2" | "C1" | "C2",
            "rawText": string | null
          },
          "workStyle": "NotSpecified" | "Onsite" | "Hybrid" | "Remote",
          "salaryRange": string | null,
          "visaSponsorshipMentioned": boolean,
          "summary": string
        }
        """;

    private const string MatchResultJsonSchema = """
        {
          "score": number,
          "strengths": string[],
          "gaps": string[],
          "suggestions": string[]
        }
        """;
}
