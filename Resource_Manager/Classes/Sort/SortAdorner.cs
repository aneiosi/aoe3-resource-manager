﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Resource_Manager.Classes.Sort
{
	public class SortAdorner(UIElement element, ListSortDirection dir) : Adorner(element), Adorner
	{
		private static Geometry ascGeometry = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");
		private static Geometry descGeometry = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");
		public ListSortDirection Direction { get; private set; } = dir;

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			if (AdornedElement.RenderSize.Width < 20)
				return;

			TranslateTransform transform = new TranslateTransform(
				AdornedElement.RenderSize.Width - 12,
				(AdornedElement.RenderSize.Height - 5) / 2
			);
			drawingContext.PushTransform(transform);

			Geometry geometry = ascGeometry;
			if (Direction == ListSortDirection.Descending)
			{
				geometry = descGeometry;
			}
			drawingContext.DrawGeometry(Brushes.Black, null, geometry);
			drawingContext.Pop();
		}
	}
}
