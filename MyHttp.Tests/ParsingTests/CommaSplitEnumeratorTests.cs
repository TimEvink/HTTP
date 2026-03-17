using System;
using System.Collections.Generic;

using MyHttp.Core.Parsing;

namespace MyHttp.Tests.Parsing {
	public class CommaSplitEnumeratorTests {
		[Theory]
		[InlineData("", 0, new string[] { })]
		[InlineData(" \t  ", 0, new string[] { })]
		[InlineData(",", 0, new string[] { })]
		[InlineData(",,", 0, new string[] { })]
		[InlineData(",  ,\t", 0, new string[] { })]
		[InlineData("a", 1, new[] { "a" })]
		[InlineData("a,b", 2, new[] { "a", "b" })]
		[InlineData("a,,c", 2, new[] { "a", "c" })]
		[InlineData(",a ,b \t", 2, new[] { "a", "b" })]
		[InlineData("a,b,", 2, new[] { "a", "b" })]
		[InlineData("a,  b c ,", 2, new[] { "a", "b c" })]
		[InlineData(" a , b ,c ", 3, new[] { "a", "b", "c" })]
		public void CommaSplitEnumerator_ShouldSplitCorrectly(string input, int expectedCount, string[] expectedTokens) {
			// Arrange
			ReadOnlySpan<byte> span = System.Text.Encoding.ASCII.GetBytes(input);
			var enumerator = new CommaSplitEnumerator(span);

			List<string> results = [];

			// Act
			while (enumerator.MoveNext()) {
				results.Add(System.Text.Encoding.ASCII.GetString(enumerator.Current));
			}

			// Assert
			Assert.Equal(expectedCount, results.Count);

			for (int i = 0; i < expectedCount; i++) {
				Assert.Equal(expectedTokens[i], results[i]);
			}
		}
	}
}
