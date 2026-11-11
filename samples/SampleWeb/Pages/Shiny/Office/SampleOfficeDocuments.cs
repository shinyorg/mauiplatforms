using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using W = DocumentFormat.OpenXml.Wordprocessing;

// MAUI's global usings bring in types with the same simple names as several WordprocessingML
// elements, so the ones that clash are pinned explicitly here.
using FontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;
using Style = DocumentFormat.OpenXml.Wordprocessing.Style;
using Underline = DocumentFormat.OpenXml.Wordprocessing.Underline;
using Shadow = DocumentFormat.OpenXml.Wordprocessing.Shadow;
using Border = DocumentFormat.OpenXml.Wordprocessing.Border;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace Sample.Features.Office;

/// <summary>
/// Builds a small .docx and .pptx in memory so the viewer demos have something real to open.
/// </summary>
/// <remarks>
/// Generated rather than shipped as binary fixtures: the samples then also demonstrate that the
/// viewers read ordinary OOXML rather than anything the library produced for itself.
/// </remarks>
public static class SampleOfficeDocuments
{
    public static byte[] BuildDocument()
    {
        using var buffer = new MemoryStream();
        using (var document = WordprocessingDocument.Create(buffer, WordprocessingDocumentType.Document, autoSave: false))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new W.Document(new Body());

            var styles = main.AddNewPart<StyleDefinitionsPart>();
            styles.Styles = new Styles(
                new DocDefaults(new RunPropertiesDefault(new RunPropertiesBaseStyle(
                    new RunFonts { Ascii = "Calibri" },
                    new FontSize { Val = "22" }))),
                new Style(
                    new StyleName { Val = "heading 1" },
                    new StyleParagraphProperties(
                        new OutlineLevel { Val = 0 },
                        new SpacingBetweenLines { Before = "280", After = "120" }),
                    new StyleRunProperties(
                        new Bold(),
                        new FontSize { Val = "34" },
                        new W.Color { Val = "1F4E79" }))
                { Type = StyleValues.Paragraph, StyleId = "Heading1" },
                new Style(
                    new StyleName { Val = "heading 2" },
                    new StyleParagraphProperties(
                        new OutlineLevel { Val = 1 },
                        new SpacingBetweenLines { Before = "240", After = "80" }),
                    new StyleRunProperties(new Bold(), new FontSize { Val = "26" }, new W.Color { Val = "2E74B5" }))
                { Type = StyleValues.Paragraph, StyleId = "Heading2" });

            var numbering = main.AddNewPart<NumberingDefinitionsPart>();
            numbering.Numbering = new Numbering(
                new AbstractNum(
                    new Level(
                        new StartNumberingValue { Val = 1 },
                        new NumberingFormat { Val = NumberFormatValues.Decimal },
                        new LevelText { Val = "%1." },
                        new PreviousParagraphProperties(new Indentation { Left = "720", Hanging = "360" }))
                    { LevelIndex = 0 },

                    // A second level whose template names the level above it, so a nested item reads
                    // as 1a rather than as a bare "a" that says nothing about which item it is under.
                    new Level(
                        new StartNumberingValue { Val = 1 },
                        new NumberingFormat { Val = NumberFormatValues.LowerLetter },
                        new LevelText { Val = "%1%2." },
                        new PreviousParagraphProperties(new Indentation { Left = "1440", Hanging = "360" }))
                    { LevelIndex = 1 })
                { AbstractNumberId = 1 },
                new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 });

            var body = main.Document.Body!;

            body.AppendChild(Styled("Heading1", "Field Notes"));
            body.AppendChild(Text(
                "This document is generated in memory by the sample and opened by DocumentView. " +
                "The viewer reflows it to whatever width the control has, so narrowing the window " +
                "re-wraps every paragraph rather than scrolling sideways."));

            body.AppendChild(Styled("Heading2", "Formatting"));
            body.AppendChild(new Paragraph(
                new Run(new RunProperties(new Bold()), new W.Text("Bold, ")),
                new Run(new RunProperties(new Italic()), new W.Text("italic, ")),
                new Run(new RunProperties(new Underline { Val = UnderlineValues.Single }), new W.Text("underlined")),
                new Run(new W.Text(" and ")),
                new Run(new RunProperties(new W.Color { Val = "C00000" }, new Bold()), new W.Text("coloured")),
                new Run(new W.Text(" runs all resolve through the style chain."))));

            body.AppendChild(new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new Italic()), new W.Text("A centred line."))));

            body.AppendChild(Styled("Heading2", "A numbered list"));
            body.AppendChild(ListItem("Numbering is resolved from numbering.xml, counters and all."));
            body.AppendChild(ListItem("Labels sit in the hanging indent, like Word draws them."));
            body.AppendChild(ListItem("Press Tab at the start of an item to nest it.", level: 1));
            body.AppendChild(ListItem("Shift+Tab brings it back out again.", level: 1));
            body.AppendChild(ListItem("Restarting a level resets everything nested inside it."));

            body.AppendChild(Text(
                "The toolbar's two list buttons turn any paragraph into a bulleted or numbered item, " +
                "and typing \"- \" or \"1. \" at the start of a line does the same thing without " +
                "reaching for them."));

            body.AppendChild(Styled("Heading2", "Spelling"));
            body.AppendChild(Text(
                "Teh editor can recieve a spell checker and show its suggestions. It is definately " +
                "worth turning on: long-press a underlined word to see what it reccomend."));

            body.AppendChild(Styled("Heading2", "A table"));
            body.AppendChild(BuildTable());

            body.AppendChild(Text(
                "Headers, footers, footnotes and comments are preserved in the package but not shown — " +
                "a reflowing view has no pages to attach them to. Ask the document for its unsupported " +
                "features and it will tell you exactly that."));

            body.AppendChild(new SectionProperties(
                new PageSize { Width = 12240, Height = 15840 },
                new PageMargin { Left = 1440, Right = 1440, Top = 1440, Bottom = 1440 }));

            main.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    static Paragraph Text(string text) => new(new Run(new W.Text(text)));

    static Paragraph Styled(string styleId, string text) => new(
        new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
        new Run(new W.Text(text)));

    static Paragraph ListItem(string text, int level = 0) => new(
        new ParagraphProperties(new NumberingProperties(
            new NumberingLevelReference { Val = level },
            new NumberingId { Val = 1 })),
        new Run(new W.Text(text)));

    static Table BuildTable()
    {
        var table = new Table(
            new TableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })),
            new TableGrid(
                new GridColumn { Width = "3600" },
                new GridColumn { Width = "2400" },
                new GridColumn { Width = "2400" }));

        table.AppendChild(new TableRow(
            new TableRowProperties(new TableHeader()),
            HeaderCell("Site"), HeaderCell("Samples"), HeaderCell("Status")));

        table.AppendChild(Row("Northern ridge", "48", "Complete"));
        table.AppendChild(Row("River delta", "31", "In progress"));
        table.AppendChild(Row("Coastal shelf", "12", "Blocked"));

        return table;
    }

    static TableRow Row(string a, string b, string c) => new(
        new TableCell(Text(a)), new TableCell(Text(b)), new TableCell(Text(c)));

    static TableCell HeaderCell(string text) => new(
        new TableCellProperties(new Shading { Fill = "DDEBF7", Val = ShadingPatternValues.Clear }),
        new Paragraph(new Run(new RunProperties(new Bold()), new W.Text(text))));

    public static byte[] BuildDeck()
    {
        using var buffer = new MemoryStream();
        using (var document = PresentationDocument.Create(buffer, PresentationDocumentType.Presentation, autoSave: false))
        {
            var presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();

            var masterPart = presentationPart.AddNewPart<SlideMasterPart>();
            masterPart.SlideMaster = BuildMaster();

            var themePart = masterPart.AddNewPart<ThemePart>();
            themePart.Theme = BuildTheme();

            var layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
            layoutPart.SlideLayout = BuildLayout();
            layoutPart.AddPart(masterPart);

            masterPart.SlideMaster.SlideLayoutIdList = new P.SlideLayoutIdList(
                new P.SlideLayoutId { Id = 2147483649U, RelationshipId = masterPart.GetIdOfPart(layoutPart) });

            var ids = new List<P.SlideId>();
            uint id = 256;

            var position = 0;

            foreach (var slide in BuildSlides())
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>();
                slidePart.Slide = slide;
                slidePart.AddPart(layoutPart);

                // Speaker notes on the slides that have something to say, so the viewer's notes strip and
                // presenting mode's Notes panel both have real content to show.
                if (position < SlideNotes.Length && SlideNotes[position].Length > 0)
                    slidePart.AddNewPart<NotesSlidePart>().NotesSlide = BuildNotes(SlideNotes[position]);

                position++;
                ids.Add(new P.SlideId { Id = id++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
            }

            presentationPart.Presentation.SlideMasterIdList = new P.SlideMasterIdList(
                new P.SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(masterPart) });

            presentationPart.Presentation.SlideIdList = new P.SlideIdList(ids.Cast<OpenXmlElement>().ToArray());
            presentationPart.Presentation.SlideSize = new P.SlideSize { Cx = 12192000, Cy = 6858000 };
            presentationPart.Presentation.NotesSize = new P.NotesSize { Cx = 6858000, Cy = 9144000 };

            presentationPart.Presentation.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    static D.Theme BuildTheme() => new(
        new D.ThemeElements(
            new D.ColorScheme(
                new D.Dark1Color(new D.SystemColor { Val = D.SystemColorValues.WindowText, LastColor = "000000" }),
                new D.Light1Color(new D.SystemColor { Val = D.SystemColorValues.Window, LastColor = "FFFFFF" }),
                new D.Dark2Color(new D.RgbColorModelHex { Val = "1F3864" }),
                new D.Light2Color(new D.RgbColorModelHex { Val = "E7E6E6" }),
                new D.Accent1Color(new D.RgbColorModelHex { Val = "2E74B5" }),
                new D.Accent2Color(new D.RgbColorModelHex { Val = "ED7D31" }),
                new D.Accent3Color(new D.RgbColorModelHex { Val = "70AD47" }),
                new D.Accent4Color(new D.RgbColorModelHex { Val = "FFC000" }),
                new D.Accent5Color(new D.RgbColorModelHex { Val = "5B9BD5" }),
                new D.Accent6Color(new D.RgbColorModelHex { Val = "C00000" }),
                new D.Hyperlink(new D.RgbColorModelHex { Val = "0563C1" }),
                new D.FollowedHyperlinkColor(new D.RgbColorModelHex { Val = "954F72" }))
            { Name = "Sample" },
            new D.FontScheme(
                new D.MajorFont(new D.LatinFont { Typeface = "Calibri Light" }),
                new D.MinorFont(new D.LatinFont { Typeface = "Calibri" }))
            { Name = "Sample" },
            new D.FormatScheme(
                new D.FillStyleList(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })),
                new D.LineStyleList(new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }))),
                new D.EffectStyleList(new D.EffectStyle(new D.EffectList())),
                new D.BackgroundFillStyleList(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })))
            { Name = "Sample" }))
    { Name = "Sample Theme" };

    static P.SlideMaster BuildMaster() => new(
        new P.CommonSlideData(
            new P.ShapeTree(
                Tree()[0], Tree()[1],
                // A footer stripe that every slide inherits from the master.
                Shape(9, "Master stripe", 0, 6400800, 12192000, 457200, D.ShapeTypeValues.Rectangle,
                    new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Accent1 })))),
        new P.ColorMap
        {
            Background1 = D.ColorSchemeIndexValues.Light1,
            Text1 = D.ColorSchemeIndexValues.Dark1,
            Background2 = D.ColorSchemeIndexValues.Light2,
            Text2 = D.ColorSchemeIndexValues.Dark2,
            Accent1 = D.ColorSchemeIndexValues.Accent1,
            Accent2 = D.ColorSchemeIndexValues.Accent2,
            Accent3 = D.ColorSchemeIndexValues.Accent3,
            Accent4 = D.ColorSchemeIndexValues.Accent4,
            Accent5 = D.ColorSchemeIndexValues.Accent5,
            Accent6 = D.ColorSchemeIndexValues.Accent6,
            Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
            FollowedHyperlink = D.ColorSchemeIndexValues.FollowedHyperlink
        },
        new P.TextStyles(
            new P.TitleStyle(new D.Level1ParagraphProperties(new D.DefaultRunProperties { FontSize = 4000 })),
            new P.BodyStyle(new D.Level1ParagraphProperties(
                new D.CharacterBullet { Char = "•" },
                new D.DefaultRunProperties { FontSize = 2000 })),
            new P.OtherStyle(new D.Level1ParagraphProperties(new D.DefaultRunProperties { FontSize = 1800 }))));

    static P.SlideLayout BuildLayout() => new(
        new P.CommonSlideData(
            new P.ShapeTree(
                Tree()[0], Tree()[1],
                Placeholder(2, "Title Placeholder", P.PlaceholderValues.Title, null,
                    838200, 400000, 10515600, 1200000,
                    new D.ListStyle(new D.Level1ParagraphProperties(
                        new D.DefaultRunProperties(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Dark2 }))
                        { FontSize = 4000, Bold = true }))),
                Placeholder(3, "Body Placeholder", P.PlaceholderValues.Body, 1U,
                    838200, 1800000, 10515600, 4200000,
                    new D.ListStyle(
                        new D.Level1ParagraphProperties(
                            new D.CharacterBullet { Char = "•" },
                            new D.DefaultRunProperties { FontSize = 2400 }),
                        new D.Level2ParagraphProperties(
                            new D.CharacterBullet { Char = "▪" },
                            new D.DefaultRunProperties { FontSize = 2000 }))))),
        new P.ColorMapOverride(new D.MasterColorMapping()))
    { Type = P.SlideLayoutValues.TitleOnly };

    static IEnumerable<P.Slide> BuildSlides()
    {
        yield return Slide(
            TitleShape("Shiny Office Viewers"),
            BodyShape(("Word and PowerPoint, read-only", 0), ("Rendered with the same Skia pipeline", 0)));

        yield return Slide(
            TitleShape("Inheritance"),
            BodyShape(
                ("Placeholders inherit from the layout", 0),
                ("Layouts inherit from the master", 0),
                ("Position, size and text style all resolve", 1)),
            Shape(20, "Callout", 838200, 5100000, 3000000, 800000, D.ShapeTypeValues.RoundRectangle,
                new D.SolidFill(new D.SchemeColor(new D.LuminanceModulation { Val = 40000 }, new D.LuminanceOffset { Val = 60000 })
                { Val = D.SchemeColorValues.Accent1 }),
                "Themed fill"));

        yield return Slide(
            TitleShape("Shapes"),
            Shape(30, "Ellipse", 900000, 2200000, 2000000, 1600000, D.ShapeTypeValues.Ellipse,
                new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Accent2 })),
            Shape(31, "Arrow", 3400000, 2400000, 2200000, 1200000, D.ShapeTypeValues.RightArrow,
                new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Accent3 })),
            Shape(32, "Star", 6200000, 2200000, 1800000, 1600000, D.ShapeTypeValues.Star5,
                new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Accent4 })),
            Shape(33, "Diamond", 8600000, 2300000, 1600000, 1400000, D.ShapeTypeValues.Diamond,
                new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Accent5 })));
    }

    /// <summary>Speaker notes, one per slide in <see cref="BuildSlides"/> order; empty means none.</summary>
    static readonly string[] SlideNotes =
    [
        "Open with why this beats embedding PowerPoint: no plugin, no licence, and the same pixels on every host.",
        "Inheritance is the part decks get wrong. A placeholder with an empty spPr still has a position - it just lives on the layout.",
        string.Empty
    ];

    static P.NotesSlide BuildNotes(string text) => new(
        new P.CommonSlideData(
            new P.ShapeTree(
                Tree()[0],
                Tree()[1],
                new P.Shape(
                    new P.NonVisualShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 2U, Name = "Notes Placeholder" },
                        new P.NonVisualShapeDrawingProperties(),
                        new P.ApplicationNonVisualDrawingProperties(
                            new P.PlaceholderShape { Type = P.PlaceholderValues.Body })),
                    new P.ShapeProperties(),
                    new P.TextBody(
                        new D.BodyProperties(),
                        new D.ListStyle(),
                        new D.Paragraph(new D.Run(new D.Text(text))))))));

    static P.Slide Slide(params OpenXmlElement[] shapes)
    {
        var tree = new P.ShapeTree(Tree()[0], Tree()[1]);
        foreach (var shape in shapes)
            tree.AppendChild(shape);

        return new P.Slide(new P.CommonSlideData(tree), new P.ColorMapOverride(new D.MasterColorMapping()));
    }

    static OpenXmlElement[] Tree() =>
    [
        new P.NonVisualGroupShapeProperties(
            new P.NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
            new P.NonVisualGroupShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()),
        new P.GroupShapeProperties(new D.TransformGroup())
    ];

    static P.Shape Shape(uint id, string name, long x, long y, long cx, long cy, D.ShapeTypeValues geometry, D.SolidFill fill, string? text = null)
    {
        var body = new P.TextBody(new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Center }, new D.ListStyle());

        if (text is not null)
        {
            body.AppendChild(new D.Paragraph(
                new D.ParagraphProperties { Alignment = D.TextAlignmentTypeValues.Center },
                new D.Run(new D.RunProperties { FontSize = 1600, Bold = true }, new D.Text(text))));
        }

        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = name },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(new D.Offset { X = x, Y = y }, new D.Extents { Cx = cx, Cy = cy }),
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = geometry },
                fill),
            body);
    }

    static P.Shape Placeholder(uint id, string name, P.PlaceholderValues type, uint? index, long x, long y, long cx, long cy, D.ListStyle listStyle)
    {
        var placeholder = new P.PlaceholderShape { Type = type };
        if (index is not null)
            placeholder.Index = index;

        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = name },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties(placeholder)),
            new P.ShapeProperties(
                new D.Transform2D(new D.Offset { X = x, Y = y }, new D.Extents { Cx = cx, Cy = cy }),
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }),
            new P.TextBody(new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Center }, listStyle));
    }

    /// <summary>A title placeholder carrying only text, so geometry and style come from the layout.</summary>
    static P.Shape TitleShape(string text) => new(
        new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = 2U, Name = "Title" },
            new P.NonVisualShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = P.PlaceholderValues.Title })),
        new P.ShapeProperties(),
        new P.TextBody(
            new D.BodyProperties(),
            new D.ListStyle(),
            new D.Paragraph(new D.Run(new D.Text(text)))));

    static P.Shape BodyShape(params (string Text, int Level)[] lines)
    {
        var body = new P.TextBody(new D.BodyProperties(), new D.ListStyle());

        foreach (var (text, level) in lines)
        {
            var paragraph = new D.Paragraph();
            if (level > 0)
                paragraph.ParagraphProperties = new D.ParagraphProperties { Level = level };

            paragraph.AppendChild(new D.Run(new D.Text(text)));
            body.AppendChild(paragraph);
        }

        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = 3U, Name = "Content" },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties(
                    new P.PlaceholderShape { Type = P.PlaceholderValues.Body, Index = 1U })),
            new P.ShapeProperties(),
            body);
    }
}
