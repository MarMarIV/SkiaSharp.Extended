﻿using System.Collections.Specialized;

namespace SkiaSharp.Extended.UI.Controls;

public class SKConfettiView : SKAnimatedSurfaceView
{
	private static readonly BindablePropertyKey IsRunningPropertyKey = BindableProperty.CreateReadOnly(
		nameof(IsRunning),
		typeof(bool),
		typeof(SKConfettiView),
		false,
		defaultBindingMode: BindingMode.OneWayToSource);

	public static readonly BindableProperty IsRunningProperty = IsRunningPropertyKey.BindableProperty;

	public static readonly BindableProperty SystemsProperty = BindableProperty.Create(
		nameof(Systems),
		typeof(SKConfettiSystemCollection),
		typeof(SKConfettiView),
		null,
		propertyChanged: OnSystemsPropertyChanged,
		defaultValueCreator: _ => CreateDefaultSystems());

	public SKConfettiView()
	{
		Themes.SKConfettiViewResources.EnsureRegistered();

		SizeChanged += OnSizeChanged;
		PropertyChanged += (_, e) =>
		{
			if (nameof(IsAnimationEnabled).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
				OnIsAnimationEnabledPropertyChanged();
		};

		IsAnimationEnabled = true;

		OnSystemsPropertyChanged(this, null, Systems);
	}

	/// <summary>
	/// Gets a value indicating whether the confetti emission is running.
	/// </summary>
	/// <remarks>
	/// NOTE this is a <see cref="BindingMode.OneWayToSource"/> property, if you wish to control whether the animation is
	/// enabled refer to the <see cref="SKAnimatedSurfaceView.IsAnimationEnabled"/> property.
	/// </remarks>
	public bool IsRunning
	{
		get => (bool)GetValue(IsRunningProperty);
		private set => SetValue(IsRunningPropertyKey, value);
	}

	public SKConfettiSystemCollection? Systems
	{
		get => (SKConfettiSystemCollection?)GetValue(SystemsProperty);
		set => SetValue(SystemsProperty, value);
	}

	protected override void Update(TimeSpan deltaTime)
	{
		if (Systems is null)
			return;

		for (var i = Systems.Count - 1; i >= 0; i--)
		{
			var system = Systems[i];
			system.Update(deltaTime);

			if (!system.IsRunning)
				Systems.RemoveAt(i);
		}
	}

	protected override void OnPaintSurface(SKCanvas canvas, SKSize size)
	{
		var particles = 0;

		if (Systems?.Count > 0)
		{
			foreach (var system in Systems)
			{
				system.Draw(canvas);

				particles += system.ParticleCount;
			}
		}

#if DEBUG
		WriteDebugStatus($"Particles: {particles}");
#endif
	}

	private void OnSizeChanged(object? sender, EventArgs e)
	{
		if (Systems is null)
			return;

		foreach (var system in Systems)
		{
			system.UpdateEmitterBounds(Width, Height);
		}
	}

	private void OnSystemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems is not null)
		{
			foreach (SKConfettiSystem system in e.NewItems)
			{
				system.UpdateEmitterBounds(Width, Height);
				system.IsAnimationEnabled = IsAnimationEnabled;
			}

			Invalidate();
		}

		UpdateIsRunning();
	}

	private void OnIsAnimationEnabledPropertyChanged()
	{
		if (Systems is null)
			return;

		foreach (var system in Systems)
		{
			system.IsAnimationEnabled = IsAnimationEnabled;
		}
	}

	private static void OnSystemsPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		if (bindable is not SKConfettiView cv)
			return;

		if (oldValue is SKConfettiSystemCollection oldCollection)
			oldCollection.CollectionChanged -= cv.OnSystemsCollectionChanged;

		if (newValue is SKConfettiSystemCollection newCollection)
			newCollection.CollectionChanged += cv.OnSystemsCollectionChanged;

		cv.UpdateIsRunning();
	}

	private void UpdateIsRunning()
	{
		if (Systems is null || Systems.Count == 0)
		{
			IsRunning = false;
			return;
		}

		var isRunning = true;
		foreach (var system in Systems)
		{
			if (!system.IsRunning)
			{
				isRunning = false;
				break;
			}
		}

		IsRunning = isRunning;
	}

	private static SKConfettiSystemCollection CreateDefaultSystems() =>
		new SKConfettiSystemCollection
		{
			new SKConfettiSystem()
		};
}
