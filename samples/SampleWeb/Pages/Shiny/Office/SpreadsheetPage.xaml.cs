using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shiny.Controls.Office.Skia;

namespace Sample.Features.Office;

public partial class SpreadsheetPage : ContentPage
{
    readonly Workbook workbook;
    bool dark;
    int nextRow = 6;

    public SpreadsheetPage()
    {
        this.InitializeComponent();

        // Built in memory rather than shipped as a binary fixture, so the demo also exercises the
        // "create a new workbook from nothing" path.
        this.workbook = Workbook.Create("Budget");
        this.Seed();

        // Nothing file-related is wired here: the backstage saves, opens and fills its recent list
        // through the document store MauiProgram registers.

        this.Sheet.Workbook = this.workbook;
    }

    void Seed()
    {
        this.Set("A1", CellValue.FromText("Item"));
        this.Set("B1", CellValue.FromText("Qty"));
        this.Set("C1", CellValue.FromText("Unit"));
        this.Set("D1", CellValue.FromText("Total"));

        this.AddItem(2, "Widget", 4, 12.50);
        this.AddItem(3, "Gadget", 2, 42.00);
        this.AddItem(4, "Doohickey", 7, 3.25);

        this.Set("A5", CellValue.FromText("Total"));
        this.workbook.Execute(new SetCellFormulaCommand("Budget", CellRef.Parse("D5"), "SUM(D2:D4)"));

        // A second sheet, so the tab strip has somewhere to go and the cross-sheet formula below has
        // something to read. Renaming Budget from its tab rewrites this formula with it.
        this.workbook.Execute(new AddSheetCommand("Summary", 1));
        this.workbook.Execute(new SetCellValueCommand("Summary", CellRef.Parse("A1"), CellValue.FromText("Budget total")));
        this.workbook.Execute(new SetCellFormulaCommand("Summary", CellRef.Parse("B1"), "Budget!D5"));

        // Notes for the Comments pane (the button at the right end of the ribbon's tab strip).
        this.workbook.Execute(new SetNoteCommand("Budget", CellRef.Parse("C3"), new CellNote(CellRef.Parse("C3"), "Price went up in March.", "Allan")));
        this.workbook.Execute(new SetNoteCommand("Summary", CellRef.Parse("B1"), new CellNote(CellRef.Parse("B1"), "Pulled from the Budget sheet.", "Allan")));
    }

    void Set(string reference, CellValue value)
        => this.workbook.Execute(new SetCellValueCommand("Budget", CellRef.Parse(reference), value));

    void AddItem(int row, string name, double quantity, double unit)
    {
        this.Set($"A{row}", CellValue.FromText(name));
        this.Set($"B{row}", CellValue.FromNumber(quantity));
        this.Set($"C{row}", CellValue.FromNumber(unit));
        this.workbook.Execute(new SetCellFormulaCommand("Budget", CellRef.Parse($"D{row}"), $"B{row}*C{row}"));
    }

    void OnAddRow(object? sender, EventArgs e)
    {
        // File ▸ New can swap in a template workbook; the button only adds to the seeded one.
        if (!ReferenceEquals(this.Sheet.Workbook, this.workbook))
            return;

        this.AddItem(this.nextRow, $"Item {this.nextRow}", this.nextRow, 5);
        this.workbook.Execute(new SetCellFormulaCommand("Budget", CellRef.Parse("D5"), $"SUM(D2:D{this.nextRow})"));
        this.nextRow++;
    }

    void OnToggleTheme(object? sender, EventArgs e)
    {
        this.dark = !this.dark;
        // null, not SpreadsheetTheme.Light: unset means "follow the app appearance", which is the
        // behaviour worth demoing. Passing Light would pin it and hide that.
        this.Sheet.Theme = this.dark ? SpreadsheetTheme.Dark : null;
    }

    void OnToggleShell(object? sender, EventArgs e) => this.Sheet.ShowShell = !this.Sheet.ShowShell;

    void OnCellChanged(object? sender, CellRef cell) { }

    // No disposal in OnHandlerChanged: Shell drops the handler on every flyout switch but keeps this
    // page, so the view came back holding a disposed workbook it was never given again. See
    // DocumentEditorPage for the iOS hang that caused.
}
