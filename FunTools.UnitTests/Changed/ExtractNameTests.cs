using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FunTools.Changed;
using NUnit.Framework;

namespace FunTools.UnitTests.Changed
{
	[TestFixture]
    [Ignore]
	public class ExtractNameTests
	{
		[Test]
		public void Can_get_property_from_func_returning_required_property()
		{
			// Arrange
			var model = new SomeModel();

			// Act
			var propertyName = ExtractName.From(() => model.Some);

			// Assert
			propertyName.Should().Be(SomeModel.SomeDataProperty);
		}

		[Test]
		public void Should_extract_only_last_property_name_in_chain()
		{
			// Arrange
			var model = new SomeModel();

			// Act
			var propertyName = ExtractName.From(() => model.Some.Other);

			// Assert
			propertyName.Should().Be("Other");
		}

		[Test]
		public void Should_be_able_to_extract_name_from_getter_delegate()
		{
			// Arrange
			var argument = "hi";

			// Act
			var extractedName = ExtractName.From(() => argument);

			// Assert
			extractedName.Should().Be("argument");
		}

		[Test]
		public void I_will_be_able_to_extract_last_name_in_property_chain()
		{
			// Arrange
			var argument = new { Data = "hi" };

			// Act
			var extractedName = ExtractName.From(() => argument.Data);

			// Assert
			extractedName.Should().Be("Data");
		}

		[Test]
		public void The_provided_cases_should_work()
		{
			// Arrange
			var array = new[] { 2 };
			var list = new List<string>(new[] { "hi" });
			var index = 0;
			var model = new SomeModel();

			// Act
			// Assert
			ExtractName.From(() => 2 + array[0])
				.Should().Be("array");

			ExtractName.From(() => 2 + list.Count)
				.Should().Be("Count");

			ExtractName.From(() => array[0] + list.First())
				.Should().Be("Concat");

			ExtractNames.From(() => list.First() + array[0]).Should()
				.ContainInOrder(new object[] {"list", "First", "array"});

			ExtractNames.From(() => model.Some.Other)
				.Should().Equal(new object[] { "model", "Some", "Other" })
				.And.HaveCount(3);

			ExtractName.From(() => array[index])
				.Should().Be("index");
		}

		[Test]
		public void Name_will_be_empty_for_constant_accessor()
		{
			// Arrange
			const int x = 3;

			// Act
			var name = ExtractName.From(() => x);

			// Assert
			name.Should().BeEmpty();
		}

		[Test]
		public void Should_throw_if_provided_getter_is_null()
		{
			AssertionExtensions.ShouldThrow<NullReferenceException>(
				() => ExtractName.From((Func<string>)null));
		}

		[Test]
		public void Should_throw_if_provided_getter_for_model_is_null()
		{
			AssertionExtensions.ShouldThrow<NullReferenceException>(
				() => ExtractName.From((Func<SomeModel, string>)null));
		}

		[Test]
		public void I_should_be_able_to_get_property_name_in_setter()
		{
			// Arrange
			var duck = new Duck();
			string propertyChanged = null;
			duck.PropertyChanged += s => propertyChanged = s;

			// Act
			duck.Color = "red";

			// Assert
			propertyChanged.Should().Be("Color");
		}

		[Test]
		public void I_should_be_able_to_get_property_name_if_property_get_with_virtual_call()
		{
			// Arrange
			var someModel = new SomeModel();

			// Act
			var propertyName = ExtractName.From(() => someModel.Some);

			// Assert
			propertyName.Should().Be("Some");
		}

		[Test]
		public void I_should_be_able_to_get_field_name()
		{
			// Arrange
			var someModel = new SomeModel();

			// Act
			var propertyName = ExtractName.From(() => someModel.Other);

			// Assert
			propertyName.Should().Be("Other");
		}

		[Test]
		public void I_should_be_able_to_get_property_name_from_it_setter()
		{
			// Arrange
			var duck = new Duck();

			// Act
			var names = ExtractNames.From(() => duck.Color = "blue");

			// Assert
			names.Should().Equal(new object[] { "duck", "Color" }).And.HaveCount(2);
		}

		[Test]
		public void I_should_be_able_to_get_event_and_source_name_from_event_subscription()
		{
			// Arrange
			var duck = new Duck();

			// Act
			var names = ExtractNames.From(() => duck.PropertyChanged += null);

			// Assert
			names.Should().Equal(new object[] { "duck", "PropertyChanged" }).And.HaveCount(2);
		}

		[Test]
		public void I_should_be_able_to_get_event_and_source_name_from_event_unsubscription()
		{
			// Arrange
			var duck = new Duck();

			// Act
			var names = ExtractNames.From(() => duck.PropertyChanged -= null);

			// Assert
			names.Should().Equal(new object[] { "duck", "PropertyChanged" }).And.HaveCount(2);
		}

		[Test]
		public void I_should_be_able_to_get_property_name_of_generic_class()
		{
			// Arrange
			var bird = new Bird<Duck>();
			string propertyChanged = null;
			bird.PropertyChanged += s => propertyChanged = s;

			// Act
			bird.Color = "red";

			// Assert
			propertyChanged.Should().Be("Color");
		}

		[Test]
		public void I_should_be_able_to_get_static_field_name()
		{
			// Act
			var name = ExtractName.From(() => Duck.Other);

			// Assert
			name.Should().Be("Other");
		}

		[Test]
		public void I_should_be_able_to_get_static_property_name()
		{
			// Act
			var name = ExtractName.From(() => Duck.Population);

			// Assert
			name.Should().Be("Population");
		}

		[Test]
		public void I_should_be_able_to_get_static_event_name()
		{
			// Act
			var name = ExtractName.From(() => Duck.PopulationChanged += null);

			// Assert
			name.Should().Be("PopulationChanged");
		}

		[Test]
		public void I_should_be_able_to_get_static_generic_method_name()
		{
			// Act
			var name = ExtractName.From(() => Duck.EvolInto<Chicken>());

			// Assert
			name.Should().Be("EvolInto");
		}

		[Test]
		public void I_should_Not_be_able_to_get_name_of_static_class_with_field_access()
		{
			// Act
			var names = ExtractNames.From(() => Duck.Other);

			// Assert
			names.Should().Equal(new object[] { "Other" }).And.HaveCount(1);
		}

		[Test]
		public void I_should_be_able_to_get_static_field_name_of_generic_class()
		{
			// Act
			var name = ExtractName.From(() => Bird<Duck>.Other);

			// Assert
			name.Should().Be("Other");
		}

		[Test]
		public void I_should_Not_be_able_to_get_names_of_generic_static_class_with_field_access()
		{
			// Act
			var names = ExtractNames.From(() => Bird<Duck>.Other);

			// Assert
			names.Should().Equal(new object[] { "Other" }).And.HaveCount(1);
		}

		[Test]
		public void I_should_be_able_to_get_static_event_name_of_generic_class()
		{
			// Act
			var name = ExtractName.From(() => Bird<Duck>.PopulationChanged += null);

			// Assert
			name.Should().Be("PopulationChanged");
		}

		[Test]
		public void I_should_be_able_to_get_static_generic_method_name_of_generic_class()
		{
			// Act
			var name = ExtractName.From(() => Bird<Duck>.EvolInto<Chicken>());

			// Assert
			name.Should().Be("EvolInto");
		}

		[Test]
		public void What_is_the_indexer_name()
		{
			// Arrange
			var model = new SomeModel();

			// Act
			var index = 3;
			var names = ExtractNames.From(() => model[index]);

			// Assert
			names.Should().Equal(new object[] { "model", "index", "Item" }).And.HaveCount(3);
		}

		[Test]
		public void When_I_try_extract_names_from_null_lambda_it_should_throw()
		{
			// Act
			// Assert
			AssertionExtensions.ShouldThrow<NullReferenceException>(
				() => ExtractNames.From((Func<bool>)null));
		}

		[Test]
		public void It_should_not_throw_when_extracting_name_from_typeof_of_T()
		{
			// Arrange
			// Act
			// Assert
			AssertionExtensions.ShouldNotThrow(() => ExtractNames.From(() => typeof(string)));
		}

		[Test]
		public void Given_method_name_with_underscore_as_4th_symbol_When_prefix_is_not_supported_Then_prefix_wont_be_stripped()
		{
			// Arrange
			// Act
			var name = ExtractName.From(() => SomeModel.bet_on_this());

			// Assert
			name.Should().StartWith("bet_");
		}

		#region CUT

		private class SomeModel
		{
			public static readonly string SomeDataProperty = typeof(SomeModel).GetProperties()[0].Name;

			public string Other = "hi";

			public SomeData Some { get; private set; }

			public string this[int index]
			{
				get { return Other + index; }
			}

			public static int bet_on_this()
			{
				return -1;
			}

			public SomeModel()
			{
				Some = null;
			}
		}

		internal class SomeData
		{
			public string Other { get; set; }
		}

		internal class Bird<TKind>
		{
			public static string Other = string.Empty;

			public static event EventHandler PopulationChanged = delegate { };

			public static T EvolInto<T>() where T : new()
			{
				return new T();
			}

			public Bird()
			{
				PopulationChanged(this, EventArgs.Empty);
			}

			public event Action<string> PropertyChanged = delegate { };

			private string _color;

			public string Color
			{
				get { return _color; }
				set
				{
					_color = value;
					PropertyChanged(ExtractName.From(() => Color));
				}
			}

			public override string ToString()
			{
				return typeof(TKind).ToString();
			}
		}

		internal class Duck
		{
			public static string Other = string.Empty;

			public static int Population { get; set; }

			public static event EventHandler PopulationChanged = delegate { };

			public static T EvolInto<T>() where T : new()
			{
				return new T();
			}

			public Duck()
			{
				++Population;
				PopulationChanged(this, EventArgs.Empty);
			}

			public event Action<string> PropertyChanged = delegate { };

			private string _color;

			public string Color
			{
				get { return _color; }
				set
				{
					_color = value;
					PropertyChanged(ExtractName.From(() => Color));
				}
			}
		}

		class Chicken
		{
		}

		#endregion
	}
}