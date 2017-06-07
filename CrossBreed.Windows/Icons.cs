using System.Windows.Media;

namespace CrossBreed.Windows {
	public static class Icons {
		public static Geometry Pin { get; } =
			Geometry.Parse("M1,14l4,-4l-3,-3l1,-1h2l3.5,-3.5a1,1 0 0,0 0,-0.5a1,1 0 0,1 1.4,-1.4l4,4a1,1 0 0,1 -1.4,1.4a1,1 0 0,0 -0.5,0l-3.5,3.5v2l-1,1l-3,-3");

		public static Geometry Image { get; } =
			Geometry.Parse("M21,19V5c0,-1.1 -0.9,-2 -2,-2H5c-1.1,0 -2,0.9 -2,2v14c0,1.1 0.9,2 2,2h14c1.1,0 2,-0.9 2,-2zM8.5,13.5l2.5,3.01L14.5,12l4.5,6H5l3.5,-4.5z");

		public static Geometry Icon { get; } = 
			Geometry.Parse("M3,5v14c0,1.1 0.89,2 2,2h14c1.1,0 2,-0.9 2,-2L21,5c0,-1.1 -0.9,-2 -2,-2L5,3c-1.11,0 -2,0.9 -2,2zM15,9c0,1.66 -1.34,3 -3,3s-3,-1.34 -3,-3 1.34,-3 3,-3 3,1.34 3,3zM6,17c0,-2 4,-3.1 6,-3.1s6,1.1 6,3.1v1L6,18v-1z");
		public static Geometry Settings { get; } =
			Geometry.Parse("M19.43,12.98c0.04,-0.32 0.07,-0.64 0.07,-0.98s-0.03,-0.66 -0.07,-0.98l2.11,-1.65c0.19,-0.15 0.24,-0.42 0.12,-0.64l-2,-3.46c-0.12,-0.22 -0.39,-0.3 -0.61,-0.22l-2.49,1c-0.52,-0.4 -1.08,-0.73 -1.69,-0.98l-0.38,-2.65C14.46,2.18 14.25,2 14,2h-4c-0.25,0 -0.46,0.18 -0.49,0.42l-0.38,2.65c-0.61,0.25 -1.17,0.59 -1.69,0.98l-2.49,-1c-0.23,-0.09 -0.49,0 -0.61,0.22l-2,3.46c-0.13,0.22 -0.07,0.49 0.12,0.64l2.11,1.65c-0.04,0.32 -0.07,0.65 -0.07,0.98s0.03,0.66 0.07,0.98l-2.11,1.65c-0.19,0.15 -0.24,0.42 -0.12,0.64l2,3.46c0.12,0.22 0.39,0.3 0.61,0.22l2.49,-1c0.52,0.4 1.08,0.73 1.69,0.98l0.38,2.65c0.03,0.24 0.24,0.42 0.49,0.42h4c0.25,0 0.46,-0.18 0.49,-0.42l0.38,-2.65c0.61,-0.25 1.17,-0.59 1.69,-0.98l2.49,1c0.23,0.09 0.49,0 0.61,-0.22l2,-3.46c0.12,-0.22 0.07,-0.49 -0.12,-0.64l-2.11,-1.65zM12,15.5c-1.93,0 -3.5,-1.57 -3.5,-3.5s1.57,-3.5 3.5,-3.5 3.5,1.57 3.5,3.5 -1.57,3.5 -3.5,3.5z");

		public static Geometry Close { get; } = Geometry.Parse("M0,0l10,10M0,10L10,0");
		public static Geometry Typing { get; } = Geometry.Parse("M6,6a2,2 0 1,1 -0.1,0zm6,0a2,2 0 1,1 -0.1,0zm6,0a2,2 0 1,1 -0.1,0z");
		public static Geometry TypingPaused { get; } = Geometry.Parse("M6,6a2,2 0 0,0 0,4h12a2,2 0 0,0 0,-4z");
		public static Geometry Link { get; } = Geometry.Parse("M2,2a2,2 0 0,0 0,8h4.5m1,0h4.5a1,1 0 0,0 0,-8h-4.5m-1,0h-4.5m0.5,4h9");
		public static Geometry Channel { get; } = Geometry.Parse("M3,13h8L11,3L3,3v10zM3,21h8v-6L3,15v6zM13,21h8L21,11h-8v10zM13,3v6h8L21,3h-8z");
		public static Geometry User { get; } = Geometry.Parse("M12,4a1,1 0 0,0 0,8a1,1 0 0,0 0,-8M12,14c-2.67,0 -8,1.34 -8,4v2h16v-2c0,-2.66 -5.33,-4 -8,-4z");
		public static Geometry Search { get; } = Geometry.Parse("M1,3a5,5 0 1,0 0.1,0zm4,9l6,6");
		public static Geometry Code { get; } = Geometry.Parse("M9,6l-6,6l6,6m5,0l6,-6l-6,-6");
		public static Geometry Repeat { get; } = Geometry.Parse("M7,7h10v3l4,-4 -4,-4v3L5,5v6h2L7,7zM17,17L7,17v-3l-4,4 4,4v-3h12v-6h-2v4z");
		public static Geometry Disable { get; } = Geometry.Parse("M12,3a9,9 0 1,0 0.1,0zm-5.5,3.5l11,11");
		public static Geometry Logs { get; } =
			Geometry.Parse("M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z");
	}
}