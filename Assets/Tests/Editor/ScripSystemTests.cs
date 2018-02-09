using NUnit.Framework;
using System.Collections.Generic;
using Script;

[TestFixture]
public class ScripSystemTests {
	private ParserContext parserContext;
	private MockWorldController scriptContext;

	[OneTimeSetUp]
	public void ScriptSystemTestContext() {
		scriptContext = new MockWorldController();
		parserContext = new ParserContext {
			Grammar = Grammar.DefaultGrammar(),
			LocalVariables = scriptContext.LocalVariables(),
			GlobalVariables = scriptContext.GlobalVariables(),
			Functions = scriptContext.Functions(),
		};
	}

	private void AssertScriptResult(ISymbol expected, string script) {
		Executable executable = Executable.FromScript(script, parserContext);
		ISymbol result = executable.Execute(scriptContext);
		Assert.AreEqual(expected.Type(), result.Type());
		Assert.AreEqual(expected.ArrayType(), result.ArrayType());
		Assert.AreEqual(expected.ValueString(), result.ValueString());
		scriptContext.LocalVariables().Clear();
	}

	[Test]
	public void ScriptSystemSimpleParsing() {
		AssertScriptResult(new IntegerSymbol(9), @"
			let a: int = 2;
			const b: int = 3;
			a * 4 + b / 2
		");
		AssertScriptResult(new FloatSymbol(-5.5f), @"
			const a: float = 5.0;
			-2.0 * a + 2.0 ^ 2.0 + 3.0 / 2.0 - 0.5 * 2.0
		");
		AssertScriptResult(new StringSymbol("string"), @"
			const a: string = 'str';
			const b: string = a + 'in';
			b + 'g'
		");
	}
}
