using Moq;
using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Pmad.Wiki.Test.Helpers;

public class WikiTemplateParameterTypeConverterTest
{
    private readonly WikiTemplateParameterTypeConverter _converter = new();

    #region Accepts

    [Fact]
    public void Accepts_WikiTemplateParameterType_ReturnsTrue()
    {
        Assert.True(_converter.Accepts(typeof(WikiTemplateParameterType)));
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(object))]
    [InlineData(typeof(WikiTemplateParameter))]
    public void Accepts_OtherTypes_ReturnsFalse(Type type)
    {
        Assert.False(_converter.Accepts(type));
    }

    #endregion

    #region ReadYaml

    [Theory]
    [InlineData("text", WikiTemplateParameterType.Text)]
    [InlineData("number", WikiTemplateParameterType.Number)]
    [InlineData("date", WikiTemplateParameterType.Date)]
    [InlineData("datetime", WikiTemplateParameterType.DateTime)]
    [InlineData("enum", WikiTemplateParameterType.Enum)]
    public void ReadYaml_KnownValues_ReturnsCorrectType(string yamlValue, WikiTemplateParameterType expected)
    {
        var parser = CreateParserWithScalar(yamlValue);

        var result = _converter.ReadYaml(parser, typeof(WikiTemplateParameterType), null!);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("TEXT")]
    [InlineData("Number")]
    [InlineData("DATE")]
    [InlineData("DateTime")]
    [InlineData("ENUM")]
    public void ReadYaml_KnownValues_CaseInsensitive(string yamlValue)
    {
        var parser = CreateParserWithScalar(yamlValue);

        var result = _converter.ReadYaml(parser, typeof(WikiTemplateParameterType), null!);

        Assert.NotNull(result);
        Assert.IsType<WikiTemplateParameterType>(result);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("checkbox")]
    [InlineData("")]
    [InlineData("int")]
    public void ReadYaml_UnknownValue_DefaultsToText(string yamlValue)
    {
        var parser = CreateParserWithScalar(yamlValue);

        var result = _converter.ReadYaml(parser, typeof(WikiTemplateParameterType), null!);

        Assert.Equal(WikiTemplateParameterType.Text, result);
    }

    [Fact]
    public void ReadYaml_NonScalarCurrent_DefaultsToTextAndAdvances()
    {
        var parser = new Mock<IParser>();
        parser.Setup(p => p.Current).Returns((ParsingEvent?)null);
        parser.Setup(p => p.MoveNext()).Returns(true);

        var result = _converter.ReadYaml(parser.Object, typeof(WikiTemplateParameterType), null!);

        Assert.Equal(WikiTemplateParameterType.Text, result);
        parser.Verify(p => p.MoveNext(), Times.Once);
    }

    [Fact]
    public void ReadYaml_AdvancesParserAfterScalar()
    {
        var parser = CreateParserWithScalar("number");
        _converter.ReadYaml(parser, typeof(WikiTemplateParameterType), null!);

        // Verify MoveNext was called exactly once (on the scalar)
        Mock.Get(parser).Verify(p => p.MoveNext(), Times.Once);
    }

    #endregion

    #region WriteYaml

    [Theory]
    [InlineData(WikiTemplateParameterType.Text, "text")]
    [InlineData(WikiTemplateParameterType.Number, "number")]
    [InlineData(WikiTemplateParameterType.Date, "date")]
    [InlineData(WikiTemplateParameterType.DateTime, "datetime")]
    [InlineData(WikiTemplateParameterType.Enum, "enum")]
    public void WriteYaml_KnownTypes_EmitsCorrectString(WikiTemplateParameterType paramType, string expectedString)
    {
        var emitter = new Mock<IEmitter>();

        _converter.WriteYaml(emitter.Object, paramType, typeof(WikiTemplateParameterType), null!);

        emitter.Verify(e => e.Emit(It.Is<Scalar>(s => s.Value == expectedString)), Times.Once);
    }

    [Fact]
    public void WriteYaml_NullValue_DoesNotEmit()
    {
        var emitter = new Mock<IEmitter>();

        _converter.WriteYaml(emitter.Object, null, typeof(WikiTemplateParameterType), null!);

        emitter.Verify(e => e.Emit(It.IsAny<ParsingEvent>()), Times.Never);
    }

    #endregion

    #region Round-trip via WikiTemplateFrontMatterParser

    [Theory]
    [InlineData("text", WikiTemplateParameterType.Text)]
    [InlineData("number", WikiTemplateParameterType.Number)]
    [InlineData("date", WikiTemplateParameterType.Date)]
    [InlineData("datetime", WikiTemplateParameterType.DateTime)]
    [InlineData("enum", WikiTemplateParameterType.Enum)]
    public void Parse_ParameterWithType_DeserializesCorrectType(string yamlType, WikiTemplateParameterType expected)
    {
        var content = $"""
            ---
            parameters:
              - name: myParam
                type: {yamlType}
            ---
            # Content
            """;

        var (frontMatter, _) = WikiTemplateFrontMatterParser.Parse(content);

        var param = Assert.Single(frontMatter.Parameters!);
        Assert.Equal(expected, param.Type);
    }

    [Fact]
    public void Parse_ParameterWithNoType_DefaultsToText()
    {
        var content = """
            ---
            parameters:
              - name: myParam
            ---
            # Content
            """;

        var (frontMatter, _) = WikiTemplateFrontMatterParser.Parse(content);

        var param = Assert.Single(frontMatter.Parameters!);
        Assert.Equal(WikiTemplateParameterType.Text, param.Type);
    }

    [Fact]
    public void Parse_ParameterWithUnknownType_DefaultsToText()
    {
        var content = """
            ---
            parameters:
              - name: myParam
                type: checkbox
            ---
            # Content
            """;

        var (frontMatter, _) = WikiTemplateFrontMatterParser.Parse(content);

        var param = Assert.Single(frontMatter.Parameters!);
        Assert.Equal(WikiTemplateParameterType.Text, param.Type);
    }

    [Fact]
    public void Parse_ParameterWithUpperCaseType_ParsesCaseInsensitively()
    {
        var content = """
            ---
            parameters:
              - name: myParam
                type: NUMBER
            ---
            # Content
            """;

        var (frontMatter, _) = WikiTemplateFrontMatterParser.Parse(content);

        var param = Assert.Single(frontMatter.Parameters!);
        Assert.Equal(WikiTemplateParameterType.Number, param.Type);
    }

    [Fact]
    public void Parse_MultipleParametersWithDifferentTypes_DeserializesAllCorrectly()
    {
        var content = """
            ---
            parameters:
              - name: title
                type: text
              - name: count
                type: number
              - name: published
                type: date
              - name: category
                type: enum
                options:
                  - news
                  - article
            ---
            # Content
            """;

        var (frontMatter, _) = WikiTemplateFrontMatterParser.Parse(content);

        Assert.Equal(4, frontMatter.Parameters!.Count);
        Assert.Equal(WikiTemplateParameterType.Text, frontMatter.Parameters[0].Type);
        Assert.Equal(WikiTemplateParameterType.Number, frontMatter.Parameters[1].Type);
        Assert.Equal(WikiTemplateParameterType.Date, frontMatter.Parameters[2].Type);
        Assert.Equal(WikiTemplateParameterType.Enum, frontMatter.Parameters[3].Type);
    }

    #endregion

    private static IParser CreateParserWithScalar(string value)
    {
        var parser = new Mock<IParser>();
        parser.Setup(p => p.Current).Returns(new Scalar(value));
        parser.Setup(p => p.MoveNext()).Returns(true);
        return parser.Object;
    }
}
