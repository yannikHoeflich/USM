# LAL (Led Animation Language) - Language

## Overview
LAL is the programming language that is used to develope the byte code for any animation on USM led devices.
As mentioned the code is compiled in a byte code, thats why this documentation is splitted in 2 parts. This page will explain how the programming language
works. If you want to learn more about the byte code,
 go to the [byte code documentation](bytecode.md).

## script structure
LAL is designed to compile in a very small byte code that is efficient and fast to process.
Thats why the language is very simple and don't support many features. 
Here are the most important things to know about LAL:
- The code for the startup has to be directly in the file, no main function
- a function always have to be defined before the first function call
- all variables are public, except for function parameters
- every statement has to end with a semicolon;

Here is a quick example of a code that makes all leds blue, you can understand that as a **Hello World**:
```
i = 0;
while i < LED_COUNT{
	Set(i, 0, 0, 255);
	i = i + 1;
};
UpdateLeds();
```

## code blocks
A code block is used for three purposes: functions, if statements and loops. A code block stats with a `{`. The open bracket is in the compiler the same a a semicolon, you can use both at both places. But it is recommended to use the curly bracket only to start a code block for `func`, `while` and `if`. 
The code block is closed with a `}`. It is still a statement, so you need to close it with a semicolon. So a code block is defined like that

```
[func, if or while statement]{
    YOUR CODE HERE
};
```

## variables
As mentioned above, variables are always public, you don't have to enter any keyword for the first declaration of a variable. The compiler automaticly checks if the variable is used before the statement and if not creates the variable automaticly. When you set a variable you have to write the variable name, then an equal sign and after that you can either just write the value or you can write a calculation there. Variables have a size of 16 Bit, that means they have a range from `-32 768` to `32 768`

### calculation
calculations are just a list of values (got my a number, variable or function). They are splitted by the operators that calculate. Calculated is from left to right, no priorisations, brackets are currently not supported.

Example:
```
number = 12 + 8 * 7;
```
The result would be `140` and that number is stored into the variable `number`

## operators
Operator|Name|Explanation
--------|----|-----------
+|Addition|Adds one value to the other
-|Subtraction|Removes right value from the left
*|Multiplication|The result is the one number times the other
/|Division|devides the left value through the right
%|Modules|Takes the right value from the left as often as it can, the value that is left is the result from this
&|Bitwise and|This uses both values as binary and compares each digit. The result will also only have there a 1 if both are also 1. **Can also be used as logical and**
\||Bitwise or|This uses both values as binary and compares each digit. The result will also only have there a 1 if one of them or both are also 1. **Can also be used as logical or**
^|Bitwise Xor|This uses both values as binary and compares each digit. The result will also only have there a 1 if one of them but not both are also 1. **Can also be used as logical xor**
=|Equals|Compares if both values are equal, if yes the result will be 1 if not than 0
!|Not Equal|Compares if both values are not equal, if yes the result will be 1 if not than 0
<|Smaller than|Compares both values and if the right one is smaller than the left, a 1 gets returned, otherwise 0
>|Bigger than|Compares both values and if the right one is bigger than the left, a 1 gets returned, otherwise 0


## loops
currently there is only the while loop in lal

### while
The while loop is runed until the given condition is false. Is starts with the keyword `while` and the condition right after that. The condition is technicly just a calculation. The loop runs until the calculation result is equals 0.

Example:
```
i = 0;
while i < 10{
    i = i + 1;
};
```

## if
The if statement runs the code inside the code block only if the condition is true. Same as with the while loop, the condition is a calculation where true means the result is greater than or equal to 0

Example:
```
if i < 5{
    i = i + 5;
};
```

## function
Functions are code parts that you can run from other parts of the code. You can give them parameter and they return a value.
### parameter
Parameters don't have a name, they are just numbered. To use a parameter, start with _ and then the index of the parameter.
Example:
```
number = _0 * 5;
```

### structure
to create a function, start writing `func` then the name of the function and start the code block.
```
func step{
    i = i + 1;
};
```

## Arrays
Arrays are not directly supported. But there is a way you can use them anyways. In the default library are the functions `CreateArray`, `SetArray` and `GetArray`.
you give CreateArray a length, then it returnes a pointer to the array, you need to store that to use the array later again. The exact implementation of the array pointer is different on any devices so you should really store them in a variable. With `SetArray` you can set one element in an array and with `GetArray` you get the value from one element.

Example:
```
length = 5;
numbers = CreateArray(length);
SetArray(numbers, 0, 3);
SetArray(numbers, 1, 33);
SetArray(numbers, 2, 45);
SetArray(numbers, 3, 12);
SetArray(numbers, 4, 1);

sum = 0;
i = 0;
while i < length{
    sum = sum + GetArray(numbers, i);
};
```

## default library
### variables
Name|explanation
----|-----------
LED_COUNT| The number of leds that is connected to the led controller
### functions

Name|explanation
----|-----------
`For lights only` Set(index, red, green, blue)`|Sets one led in the led bFffer 
`For matrix only` Set(x, y, red, green, blue)|Sets one led in the matrix buffer 
UpdateLeds()|Sends the led buffer to the leds, so they are visible in real
Random()|A random value in the range from the variable (`-32 768` to `32 768`)
CreateArray(length)|Creates an array with the given size, returns the array pointer
SetArray(pointer, index, value)|sets one value at the index of the array with that pointer
GetArray(pointer, index)| Gets the value which is stored at that index of the array with that pointer
SleepFrame(milliseconds)|Waits the time in milliseconds but subtracts the time that has past since last call of this function
Sleep(milliseconds)|Just waits that amount of milliseconds
GetTime()|Get the current time on the clock in minutes that have past at that day