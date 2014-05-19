# WPF Dashboard

A library of simple dashboard controls for WPF.

Currently only has one control: Dial360.

## Dial360 Usage

Add a reference to the Dashboard project and embed the Dial360 control like this:

    <dashboard:Dial360 Minimum="{Binding Min}"
                       Maximum="{Binding Max}"
                       Value="{Binding Value}" />

Dial360 uses the `INotifyPropertyChanged` interface to monitor the bindings. 
Whenever the Value property changes the needle is animated to the new value.

By default the Dial tries to intelligently create a range of "notches" based on 
the range of values between `Minimum` and `Maximum`. You can override this behaviour
by instead explicitly binding an `IEnumerable<Dial360Notch>` collection to the `Notches`
property.

    <dashboard:Dial360 
               ...
               Notches="{Binding MyNotches}" />

Where MyNotches might be defined as...

    ... = new ObservableCollection<Dial360Notch> {
        new Dial360Notch(label: "A", angle: -150),
        new Dial360Notch(label: "B", angle: 150)
    };

If the collection implements `INotifyCollectionChanged` then Dial360 monitors this collection
for changes directly.

All of the properties which can be bound are:

1. `Value` (`double`) - the current "value" of the needle.
2. `AnimationDuration` (`TimeSpan`) - the duration of the needle animation when Value updates.
3. `Minimum` (`double`) - the minimum value expected from Value. Animations are clamped to this.
4. `Maximum` (`double`) - the maximum value expected from Value. Animations are clamped to this.
5. `Label` (`string`) - the text rendered in the center of the dial. 
6. `Notches` (`IEnumerable<Dial360Notch>`) - the collection of notches which describe the range of the dial.
7. `DefaultNotchCount` (`int`) - the number of notches rendered if the `Notches` property is not set.
