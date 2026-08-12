Based on your answers, I would change the PoC architecture to explicitly prove a **strongly typed polymorphic model + server-side Razor model binding + JSON persistence**, while using an **in-memory store for the multi-step draft state**.

## Updated PoC Objective

Prove this end-to-end flow:

```text
Step 1: Request Basic Data
          ↓
Step 2: Exhibits
          ↓
       Evidences
          ↓
       Inspections
          ↓
Step 3: Dynamically determine
        strongly typed inspection model
          ↓
Server Model Binding
          ↓
Base Inspection Model
    + derived models
          ↓
Serialize derived model to JSON
          ↓
Persist in DB JSON column
```

The key architectural principle is:

> **The application uses strongly typed C# models internally, while persistence uses a JSON column for the polymorphic inspection data.**

---

# 1. Proposed Domain Model

I would keep the core hierarchy strongly typed and stable.

```text
Request
 ├── Id
 ├── BasicData
 └── Exhibits[]
      └── Exhibit
           ├── Id
           ├── EvidenceType
           └── Evidences[]
                └── Evidence
                     ├── Id
                     ├── EvidenceType
                     └── Inspections[]
                          └── Inspection
                               ├── Id
                               ├── InspectionType
                               ├── EvidenceType
                               └── Data
```

The important part is `Inspection.Data`.

---

# 2. Base Type + Derived Types

Use one base class:

```csharp
public abstract class InspectionData
{
}
```

Then specific models:

```csharp
public class ChemicalAnalysisInspectionData : InspectionData
{
    public string? SubstanceName { get; set; }
    public decimal? SampleWeight { get; set; }
    public string? ChemicalComposition { get; set; }
    public string? Result { get; set; }
}
```

```csharp
public class FingerprintInspectionData : InspectionData
{
    public string? FingerprintClassification { get; set; }
    public string? NumberOfPrints { get; set; }
    public string? Findings { get; set; }
}
```

```csharp
public class BallisticInspectionData : InspectionData
{
    public string? WeaponType { get; set; }
    public string? Caliber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Findings { get; set; }
}
```

Then:

```csharp
public class Inspection
{
    public Guid Id { get; set; }

    public EvidenceType EvidenceType { get; set; }

    public InspectionType InspectionType { get; set; }

    public InspectionData Data { get; set; } = default!;
}
```

This gives you exactly the model you described:

```text
Inspection
   │
   └── InspectionData
          │
          ├── ChemicalAnalysisInspectionData
          ├── FingerprintInspectionData
          ├── BallisticInspectionData
          └── ...
```

---

# 3. Combination Determines Concrete Model

The combination:

```text
EvidenceType + InspectionType
```

determines the concrete C# model.

For example:

```text
Evidence = Substance
Inspection = Chemical Analysis
                    ↓
ChemicalAnalysisInspectionData
```

and:

```text
Evidence = Weapon
Inspection = Ballistic Examination
                    ↓
BallisticInspectionData
```

I recommend implementing a dedicated resolver:

```csharp
public interface IInspectionDataTypeResolver
{
    Type Resolve(
        EvidenceType evidenceType,
        InspectionType inspectionType);
}
```

Example:

```csharp
public Type Resolve(
    EvidenceType evidenceType,
    InspectionType inspectionType)
{
    return (evidenceType, inspectionType) switch
    {
        (EvidenceType.Substance, InspectionType.ChemicalAnalysis)
            => typeof(ChemicalAnalysisInspectionData),

        (EvidenceType.Weapon, InspectionType.Ballistic)
            => typeof(BallisticInspectionData),

        (EvidenceType.Document, InspectionType.Handwriting)
            => typeof(HandwritingInspectionData),

        _ => throw new BusinessException(
            "Unsupported inspection combination.")
    };
}
```

This becomes a key component of the PoC.

---

# 4. Step 1

Keep Step 1 simple.

```text
Create Request
    ↓
Basic Information
    ↓
Save Draft
    ↓
Next
```

Example:

```csharp
public class RequestBasicData
{
    public string RequestNumber { get; set; }
    public string Subject { get; set; }
    public DateTime RequestDate { get; set; }
}
```

The objective here is mainly to prove the multi-step lifecycle.

---

# 5. Step 2 – Hierarchical Dynamic Collection

Step 2 should allow the user to build:

```text
Exhibit
   └── Evidence
          ├── Inspection
          ├── Inspection
          └── Inspection
```

For example:

```text
Exhibit #1
  Evidence: Weapon
      Inspection: Ballistic
      Inspection: Fingerprint

  Evidence: Document
      Inspection: Handwriting
```

Because your structure is fixed after Step 2, I would **not** try to make Step 2 dynamically render different data-entry forms.

Step 2 only captures:

```text
Exhibit
Evidence
Evidence Type
Inspection
Inspection Type
```

Then Step 3 is responsible for the actual inspection-specific data.

---

# 6. Step 3 – Strongly Typed Dynamic Pages

This is the main part of the PoC.

When the user reaches Step 3:

```text
Request
   ↓
Exhibit
   ↓
Evidence
   ↓
Inspection
   ↓
EvidenceType + InspectionType
   ↓
Resolve concrete model
   ↓
Render corresponding Razor Page/Partial
```

For example:

```text
Inspection #1
Evidence Type: Weapon
Inspection Type: Ballistic

        ↓

BallisticInspection.cshtml
BallisticInspectionData
```

Another inspection:

```text
Inspection #2
Evidence Type: Document
Inspection Type: Handwriting

        ↓

HandwritingInspection.cshtml
HandwritingInspectionData
```

This provides a clean demonstration that different inspection models can coexist within the same Request.

---

# 7. Razor Model Binding

Since you specifically want **server-side model binding**, I would make that a major PoC acceptance criterion.

For example:

```csharp
public class BallisticInspectionModel : PageModel
{
    [BindProperty]
    public BallisticInspectionData Data { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Save inspection

        return RedirectToPage(...);
    }
}
```

Then Razor handles the strongly typed properties:

```html
<input asp-for="Data.WeaponType" />
<input asp-for="Data.Caliber" />
<input asp-for="Data.SerialNumber" />
<textarea asp-for="Data.Findings"></textarea>
```

This is preferable for the PoC to manually processing `Request.Form` or building generic dictionaries.

---

# 8. In-Memory Draft Store

For the PoC, I strongly recommend introducing an **in-memory draft/session store**.

The purpose is to demonstrate multi-step state without forcing every change immediately into the database.

For example:

```csharp
public interface IRequestDraftStore
{
    Task<RequestDraft?> GetAsync(Guid requestId);

    Task SaveAsync(
        Guid requestId,
        RequestDraft draft);

    Task RemoveAsync(Guid requestId);
}
```

Implementation:

```csharp
ConcurrentDictionary<Guid, RequestDraft>
```

The draft could contain:

```text
RequestDraft
 ├── BasicData
 ├── Exhibits[]
 │    └── Evidences[]
 │         └── Inspections[]
 │              ├── EvidenceType
 │              ├── InspectionType
 │              └── InspectionData
```

This lets you demonstrate:

```text
Step 1
   ↓
In-memory draft
   ↓
Step 2
   ↓
Update draft
   ↓
Step 3
   ↓
Update draft
   ↓
Submit
   ↓
Persist to database
```

---

# 9. Important: In-Memory Store Should Not Replace DB

I would use the in-memory store specifically as the **PoC draft/session state**, not as the final persistence architecture.

For example:

```text
                 ┌─────────────────────┐
                 │   In-Memory Draft   │
                 │                     │
Step 1 ─────────►│ Basic Data          │
Step 2 ─────────►│ Exhibits/Evidences  │
Step 3 ─────────►│ Inspection Data     │
                 └──────────┬──────────┘
                            │
                         Submit
                            │
                            ▼
                 ┌─────────────────────┐
                 │      Database       │
                 │                     │
                 │ Request             │
                 │ Exhibit             │
                 │ Evidence            │
                 │ Inspection          │
                 │ InspectionData JSON │
                 └─────────────────────┘
```

This also makes the PoC architecture very easy to demonstrate.

---

# 10. Database Design

I recommend keeping the relational structure for the hierarchy:

```text
Request
Exhibit
Evidence
Inspection
```

but storing the variable inspection model in a JSON column.

For example:

```text
Inspection
------------------------------------------------
Id
EvidenceId
InspectionType
EvidenceType
InspectionDataJson
CreationTime
...
```

The JSON could contain:

```json
{
  "weaponType": "Pistol",
  "caliber": "9mm",
  "serialNumber": "ABC123",
  "findings": "..."
}
```

Another inspection row could contain:

```json
{
  "substanceName": "Sample A",
  "sampleWeight": 14.5,
  "chemicalComposition": "...",
  "result": "Positive"
}
```

Same database column, completely different strongly typed object.

---

# 11. Serialization Strategy

The PoC should explicitly demonstrate:

```text
C# Derived Model
       ↓
JSON serialization
       ↓
InspectionDataJson
```

and when loading:

```text
InspectionDataJson
       ↓
EvidenceType + InspectionType
       ↓
Resolve Type
       ↓
JSON deserialization
       ↓
Derived C# Model
```

For example:

```csharp
var type = resolver.Resolve(
    inspection.EvidenceType,
    inspection.InspectionType);

var data = JsonSerializer.Deserialize(
    inspection.InspectionDataJson,
    type);
```

I would also investigate using a small discriminator in the JSON, e.g.:

```json
{
  "$type": "Ballistic",
  "weaponType": "Pistol",
  "caliber": "9mm"
}
```

However, I would **not rely on raw CLR type names** in persisted JSON. The PoC should use a controlled business discriminator such as the inspection combination.

---

# 12. Reset Behavior

Your answer to question 8 is important.

If Step 2 changes after Step 3 has been entered:

```text
Step 3 data
     ↓
Step 2 modified
     ↓
Inspection combination changed
     ↓
Invalidate/reset Step 3
```

I would implement this explicitly.

For example:

```text
Step 3 Status
----------------
NotStarted
InProgress
Completed
Invalidated
```

When an Evidence/Inspection definition changes:

```csharp
draft.InvalidateInspectionData();
```

Then Step 3 starts again using the new combination.

For the PoC, this should be one of the demonstrated scenarios.

---

# 13. Conditional Fields

Since conditional fields **may** be required, I would include one representative example but avoid building a full generic rules engine.

For example:

```text
Result = Positive
        ↓
Show:
AdditionalFindings
RequiredTests
```

This can initially be implemented directly in the strongly typed Razor Page:

```javascript
document.getElementById(...)
```

or server-side validation:

```csharp
if (Data.Result == Result.Positive &&
    string.IsNullOrEmpty(Data.AdditionalFindings))
{
    ModelState.AddModelError(...);
}
```

The PoC should demonstrate the capability without over-engineering it.

---

# 14. ABP Structure

I would organize the PoC approximately like this:

```text
Domain
│
├── Requests
│   ├── Request
│   ├── Exhibit
│   ├── Evidence
│   ├── Inspection
│   └── InspectionData
│
├── InspectionTypes
│
└── EvidenceTypes


Application
│
├── Requests
│   ├── RequestAppService
│   └── RequestDto
│
├── Inspections
│   ├── InspectionAppService
│   ├── InspectionDataTypeResolver
│   └── InspectionDataSerializer
│
└── Drafts
    └── RequestDraftStore


EntityFrameworkCore
│
├── Request
├── Exhibit
├── Evidence
└── Inspection
       └── InspectionDataJson


Web
│
└── Pages
    └── Requests
        ├── Create
        ├── Step1
        ├── Step2
        ├── Step3
        │   ├── Ballistic
        │   ├── Fingerprint
        │   ├── ChemicalAnalysis
        │   └── ...
        └── Review
```

---

# 15. Step 3 Page Strategy

I recommend **one host Step 3 page + individual strongly typed partials/pages**.

Conceptually:

```text
Step3.cshtml
    │
    ├── Resolve inspection combination
    │
    └── Render:
         ├── _BallisticInspection.cshtml
         ├── _FingerprintInspection.cshtml
         ├── _ChemicalAnalysisInspection.cshtml
         └── ...
```

But each partial should be strongly typed:

```csharp
@model BallisticInspectionData
```

rather than:

```csharp
@model dynamic
```

This gives you:

* compile-time validation
* IntelliSense
* normal Razor `asp-for`
* standard server-side model binding
* easier validation
* clearer code
* easier future maintenance

---

# 16. End-to-End PoC Scenarios

I would make the PoC demonstrate these scenarios:

### Scenario 1 – Basic flow

```text
Create Request
   ↓
Enter Basic Data
   ↓
Create Exhibit
   ↓
Create Evidence
   ↓
Create Inspection
   ↓
Step 3
   ↓
Enter inspection-specific data
   ↓
Submit
   ↓
Verify database
```

### Scenario 2 – Multiple inspection types

One Evidence:

```text
Weapon
 ├── Ballistic
 └── Fingerprint
```

Demonstrate that the two inspections render two completely different strongly typed forms.

### Scenario 3 – Multiple evidence types

```text
Exhibit
 ├── Weapon
 │     ├── Ballistic
 │     └── Fingerprint
 │
 └── Document
       └── Handwriting
```

This proves both dimensions of the type resolution.

### Scenario 4 – Draft

```text
Step 1
 → Save
 → Close

Reopen
 → Continue Step 2
 → Continue Step 3
```

### Scenario 5 – Reset

```text
Step 3 completed
       ↓
Return to Step 2
       ↓
Change inspection configuration
       ↓
Step 3 data invalidated
       ↓
New strongly typed form
```

### Scenario 6 – Persistence

Verify that:

```text
C# derived model
        ↓
JSON
        ↓
DB
        ↓
DB read
        ↓
correct derived C# model
```

---

# 17. Recommended PoC Boundaries

To keep the PoC focused, I would deliberately exclude:

```text
Authentication/authorization customization
File attachments
Workflow engine
Notifications
Audit framework customization
Generic dynamic-form designer
Admin UI for form configuration
Generic JSON schema engine
Large-scale performance testing
```

The objective is to prove the **technical pattern**, not build a framework.

---

# 18. Final PoC Architecture

The architecture I would ultimately demonstrate is:

```text
                         Razor Pages
                              │
                     Server Model Binding
                              │
                              ▼
                    ┌───────────────────┐
                    │ Step 3 Host Page  │
                    └─────────┬─────────┘
                              │
                  EvidenceType +
                  InspectionType
                              │
                              ▼
                ┌────────────────────────┐
                │ Type Resolver          │
                │                        │
                │ Combination → C# Type  │
                └────────────┬───────────┘
                             │
                 ┌───────────┴────────────┐
                 ▼                        ▼
       BallisticData             ChemicalAnalysisData
       FingerprintData           HandwritingData
       ...
                 │
                 ▼
       InspectionData base type
                 │
                 ▼
          JSON Serialization
                 │
                 ▼
       InspectionDataJson
                 │
                 ▼
              SQL DB
```

This is a good fit for your requirement because **the DB schema remains stable even as the inspection models differ substantially**, while the application remains strongly typed rather than becoming a generic dictionary/dynamic-form implementation.

### One additional design decision I recommend for the PoC

Use a deterministic mapping based on the business combination:

```csharp
(EvidenceType, InspectionType)
        → InspectionData Type
        → Razor component/partial
        → Validator
```

rather than scattering `switch` statements across the Razor Pages. That gives you one place to manage the relationship between the fixed master data and the concrete models.

The next useful artifact would be the **actual PoC technical design**, including the ABP entities, EF Core JSON-column mapping, `RequestDraftStore`, type resolver, sample models, Razor Page model-binding implementation, and the Step 1–3 sequence.
