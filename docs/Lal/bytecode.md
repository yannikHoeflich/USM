# LAL (Led Animation Language) - Byte Code

## Overview
LAL is the programming language that is used to develope the byte code for any animation on USM led devices.
As mentioned the code is compiled in a byte code, thats why this documentation is splitted in 2 parts. This page will explain how the byte code
works. If you want to learn more about the language, go to the [language documentation](bytecode.md).


## structure
The byte code is splitted in parts with the size of either 1 or 2 bytes. Which size the information has, depends on the situation.

## data
### byte code
The byte code is a list of bytes. It can't modify it self.

### memory
Every variable is a 16 Bit integer and has a index. Names aren't used here anymore.

## instructions

### 0 - exit function
Ends the current function and returns a value. If this is in the main code, the code ends.
Value|Size|Description
-----|----|-----------
`0`  |1B  |Instruction identifying byte
return value|?B|A calculation as defined later

### 1 - set variable
Sets a public variable to a value that is calculated here.
Value|Size|Description
-----|----|-----------
`1`  |1B  |Instruction identifying byte
Variable Index|2B|The index of the variable to set
value|?B|A calculation as defined later

### 2 - jump
Jumps to a specific point in the byte code, if the given variable is `0`
Value|Size|Description
-----|----|-----------
`2`  |1B  |Instruction identifying byte
Variable Index|2B|The variable which value is checked if its 0
New Position|2B|The new position in the bytecode, from where the code is executed 

### 3 - get a value
This is mainly for functions that are called without setting the returned value to a variable.
Value|Size|Description
-----|----|-----------
`3`  |1B  |Instruction identifying byte
Value|?B|gets a value as defined later

### 4 - set local parameter
Sets a local parameter in a function to a value that is calculated here.

Value|Size|Description
-----|----|-----------
`4`  |1B  |Instruction identifying byte
parameter Index|2B|The index of the parameter to set
value|?B|A calculation as defined later

## Get value
A value is splitted into two parts. The first one is the source of the value (number, variable, function, parameter). The second one is the value.

### 0 - number
Value|Size|Description
-----|----|-----------
`0`  |1B  |value identifying byte
value|2B|Just the number that we want

### 1 - variable
Value|Size|Description
-----|----|-----------
`1`  |1B  |value identifying byte
variable index|2B|The variable with that index is used

### 2 - function
This runs a function and gets the returned value as value used here. If the function pointer is negative, it is one from the default library and should be implemented the the source code of the runtime

Value|Size|Description
-----|----|-----------
`2`  |1B  |value identifying byte
function pointer|2B|The position in bytecode where the function starts
parameter amount|2B|The amount of parameters that need to be read next
parameters|?B|The parameters are each a value on they own. They need to pe parsed the same as this one.

### 3 - parameter
Value|Size|Description
-----|----|-----------
`3`  | 1B |value identifying byte
parameter index|2B|The parameter with that index is used

## Calculation
A calculation starts with the amount of values that are calculated together. It starts with zero and each part then have a type which determines how the part is calculated to the final result. They are then calculated from left to right.

Value|Size|Description
-----|----|-----------
calculation part amount |1B |The amound of parts that the calculation needs to use
calculation parts|?B|The calculation parts, how every one of them is build, is defined next

Value|Size|Description
-----|----|-----------
calculation type | 1B | The calculation type, you can read in the next table what the value means 
value|?B|The value, how it is explained earlier

Byte value (hex)|Operator|Name|Explanation
----------|--------|----|-----------
00|+|Addition|Adds one value to the other
01|-|Subtraction|Removes right value from the left
02|*|Multiplication|The result is the one number times the other
03|/|Division|devides the left value through the right
04|%|Modules|Takes the right value from the left as often as it can, the value that is left is the result from this
05|&|Bitwise and|This uses both values as binary and compares each digit. The result will also only have there a 1 if both are also 1. **Can also be used as logical and**
06|\||Bitwise or|This uses both values as binary and compares each digit. The result will also only have there a 1 if one of them or both are also 1. **Can also be used as logical or**
07|^|Bitwise Xor|This uses both values as binary and compares each digit. The result will also only have there a 1 if one of them but not both are also 1. **Can also be used as logical xor**
08|=|Equals|Compares if both values are equal, if yes the result will be 1 if not than 0
09|!|Not Equal|Compares if both values are not equal, if yes the result will be 1 if not than 0
0A|<|Smaller than|Compares both values and if the right one is smaller than the left, a 1 gets returned, otherwise 0
0B|>|Bigger than|Compares both values and if the right one is bigger than the left, a 1 gets returned, otherwise 0

## Example implementation
This section explaines a way to implement all of this requirements in to your programming language. This is just one way, if you want to do it partly or completely different than this, there is no problem with that. 
For better understanding, when we speak of "functions" that always means. functions in the byte code. Functions/Methods in your programming language that you have to create or use we always use the work "method".

### Examples
In all the examples below will use c++. We assume that you already have byte defined as an unsigned 16 Bit integer. If not define it like that at the beginning of your file:
```cpp
#define byte unsigned char
```
As 16 Bit integer `short` will be used. Ensure that this is the case on your machine, otherwise use `int16_t`

### basic functionality
First of all we work with a pointer that stores the current position in the byte code. Every method that gets data from the byte code, we increase the position by the amount of bytes that have been read. To do that in multiple methods we use a reference, that is one parameter in every method. 
We have mainly two types of data that we can get: 1 Byte and 2 Bytes. We create for each of them a method that gets the value, increase the pointer by 1 or 2 and returns the value. That is actually the only way how the program will read data from the byte code.

example implementation:
```cpp
byte getByte(short& pos){
    byte value = byteCode[pos];
    pos++;
    return value;
}
short getInt(short& pos){
    short value = byteCode[pos] + (byteCode[pos + 1] << 8);
    pos += 2;
    return value;
}
```


### storing data
#### byte code
The byte code is not allowed to change it self. Thats why it can be stored in the  and can actually be executed by multiple thread at the time of execution. We use the byte array `byteCode` here.

```c++
byte byteCode[5000];
```
#### variables & parameters
Where you store variables depends if you want to run with multiple threads over the byte code, you should store it just as method parameters, otherwise it can be in the class too. We will use the first way, because that is possible in all cases. Parameters always have to be in the method parameters, because it depends on the function call we are currently in, which parameters we have.

```cpp
void exampleMethod(short& pos, short variables[100], short parameters[100]){
    // do something
}
```
#### arrays
Arrays aren't implemented directly in the language, it uses functions to create an array and get or set a value. At this point we will only describe a ways to store the values, not how the create, get and set functions are implemented. We will use a short array with a fixed size and a variable that stores how much of that is already used. When a new array is requested, that variable will get increased by the size of the array. That big "arrayBuffer" can also be in the class or function parameter, due to the fact that arrays are only used in very few parts, we will store it in the class.  

```cpp
short usedFromArray = 0;
short arrayBuffer[10000];
```

### function
Now we have the basics ready to implement the execution of the byte code. Above we explained that at every method we will have the position as reference. Functions are the only exception for that, because function will get called recursive. That way the old position is always stored in the time the function is executed.

```cpp
short callFunction(short pos, short variables[100], short parameters[100]){
    // code here later
}
```

The first thing the function method needs to check if the position is negative, because then it would be a function in the default library. That is just a simple switch statement or .

```cpp
if(pos < 0){
    switch(pos){
        case -1:
        setLeds(parameters[0],parameters[1],parameters[2],parameters[3]);
        return 0;

        case -2:
        updateLeds();
        return 0;

        case -3:
        return randomShort();

        case -4:
        return createArray(parameters[0]);

        case -5:
        setArray(parameters[0], parameters[1], parameters[2])
        return 0;

        case -6:
        return getArray(parameters[0], parameters[1]);

        case -7:
        waitFrame(parameters[0]);
        return 0;

        case -8:
        wait(parameters[0]);
        return 0;

        case -9:
        return getMinutesOfDay();
    }
    return 0;
}
```

### main loop
The main loop is just a loop that runs until the position is out of the byte code length. If you want any other cancel conditions, do they also in the head of that while loop. The loop just checks what the next instruction is and calls the right method.

```cpp
while(pos < byteCodeLength){
    byte instruction = getByte(pos);
    if(instruction == 0){
        return calculate(pos, variable, parameters);
    } else if(instruction == 1){
        setVariable(pos, variable, parameters);
    } else if(instruction == 2){
        jump(pos, variable, parameters);
    } else if(instruction == 3){
        getValue(pos, variable, parameters);
    } else if(instruction == 4){
        setParameter(pos, variable, parameters);
    }
}
```

The finished function call method is this:

```cpp
short callFunction(short pos, short variables[100], short parameters[100]){

    if(pos < 0){
        switch(pos){
            case -1:
            setLeds(parameters[0],parameters[1],parameters[2],parameters[3]);
            return 0;

            case -2:
            updateLeds();
            return 0;

            case -3:
            return randomShort();

            case -4:
            return createArray(parameters[0]);

            case -5:
            setArray(parameters[0], parameters[1], parameters[2])
            return 0;

            case -6:
            return getArray(parameters[0], parameters[1]);

            case -7:
            waitFrame(parameters[0]);
            return 0;

            case -8:
            wait(parameters[0]);
            return 0;

            case -9:
            return getMinutesOfDay();
        }
        return 0;
    }

    while(pos < byteCodeLength){
        byte instruction = getByte(pos);
        if(instruction == 0){
            return calculate(pos, variable, parameters);
        } else if(instruction == 1){
            setVariable(pos, variable, parameters);
        } else if(instruction == 2){
            jump(pos, variable, parameters);
        } else if(instruction == 3){
            getValue(pos, variable, parameters);
        } else if(instruction == 4){
            setParameter(pos, variable, parameters);
        }
    }
}
```

### getting a value

### process a calculation