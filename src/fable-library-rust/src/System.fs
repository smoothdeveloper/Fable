module System

open Global_
open System.Collections.Generic

type Array() = class end
type Enum() = class end

type Exception(message: string) =
    new () = Exception("")
    member _.Message =
        if System.String.IsNullOrEmpty(message)
        then "Specified argument was out of the range of valid values."
        else message

type InvalidOperationException(message: string) =
    new () = InvalidOperationException("")
    member _.Message =
        if System.String.IsNullOrEmpty(message)
        then "Operation is not valid due to the current state of the object."
        else message

type ArgumentException(message: string, paramName: string) =
    new () = ArgumentException("", "")
    new (message) = ArgumentException(message, "")
    member _.Message =
        let message =
            if System.String.IsNullOrEmpty(message)
            then "Value does not fall within the expected range."
            else message
        if System.String.IsNullOrEmpty(paramName)
        then message
        else message + " (Parameter '" + paramName + "')"
    member _.ParamName = paramName

type ArgumentOutOfRangeException(paramName: string, message: string) =
    new () = ArgumentOutOfRangeException("", "")
    new (paramName) = ArgumentOutOfRangeException(paramName, "")
    member _.Message =
        let message =
            if System.String.IsNullOrEmpty(message)
            then "Specified argument was out of the range of valid values."
            else message
        if System.String.IsNullOrEmpty(paramName)
        then message
        else message + " (Parameter '" + paramName + "')"
    member _.ParamName = paramName

module Collections =

    module Generic =

        // type Comparer<'T when 'T: comparison>() =
        //     static member Default = Comparer<'T>()
        //     interface IComparer<'T> with
        //         member _.Compare(x, y) = LanguagePrimitives.GenericComparison x y

        // type EqualityComparer<'T when 'T: equality>() =
        //     static member Default = EqualityComparer<'T>()
        //     interface IEqualityComparer<'T> with
        //         member _.Equals(x, y) = LanguagePrimitives.GenericEquality x y
        //         member _.GetHashCode(x) = LanguagePrimitives.GenericHash x

        type Stack<'T when 'T: equality> private (initialContents: 'T[], initialCount) =
            let mutable contents = initialContents
            let mutable count = initialCount

            let toSeq() =
                let count = count
                let contents = contents
                seq { for i = count - 1 downto 0 do contents[i] }

            new (initialCapacity: int) =
                let arr = Array.zeroCreate<'T>(initialCapacity)
                Stack<'T>(arr, 0)

            new () = Stack<'T>(4)

            new (xs: IEnumerable<'T>) =
                let arr = Array.ofSeq xs
                Stack<'T>(arr, arr.Length)

            member _.Ensure(newSize) =
                let oldSize = contents.Length
                if newSize > oldSize then
                    let old = contents
                    contents <- Array.zeroCreate (max newSize (oldSize * 2))
                    Array.blit old 0 contents 0 count

            member _.Count = count

            member _.Pop() =
                count <- count - 1
                contents[count]

            member _.Peek() =
                contents[count - 1]

            member _.Contains(x: 'T) =
                let mutable found = false
                let mutable i = 0
                while i < count && not found do
                    if x = contents[i] then
                        found <- true
                    else
                        i <- i + 1
                found

            member this.TryPeek(result: 'T byref) =
                if count > 0
                then
                    result <- this.Peek()
                    true
                else
                    false

            member this.TryPop(result: 'T byref) =
                if count > 0
                then
                    result <- this.Pop()
                    true
                else
                    false

            member this.Push(x) =
                this.Ensure(count + 1)
                contents[count] <- x
                count <- count + 1

            member _.Clear() =
                count <- 0
                Array.fill contents 0 contents.Length Unchecked.defaultof<_>

            member this.TrimExcess() =
                if float count / float contents.Length > 0.9
                then
                    this.Ensure(count)

            member _.ToArray() =
                let res = ResizeArray<_>(count)
                for i = 0 to count - 1 do
                    res.Add(contents[count - 1 - i])
                res |> asArray

            interface IEnumerable<'T> with
                member _.GetEnumerator(): IEnumerator<'T> =
                    toSeq().GetEnumerator()

            interface System.Collections.IEnumerable with
                member _.GetEnumerator(): System.Collections.IEnumerator =
                    toSeq().GetEnumerator() :> System.Collections.IEnumerator

        type Queue<'T when 'T: equality> private (initialContents, initialCount) =
            let mutable contents: 'T array = initialContents
            let mutable count = initialCount
            let mutable head = 0
            let mutable tail = initialCount

            let size() = contents.Length

            let toIndex i = (head + i) % size()

            let ensure(requiredSize: int) =
                let newBuffer: 'T array = Array.zeroCreate requiredSize

                if head < tail then
                    Array.blit contents head newBuffer 0 count
                else
                    Array.blit contents head newBuffer 0 (size() - head)
                    Array.blit contents 0 newBuffer (size() - head) tail

                head <- 0
                tail <- count
                contents <- newBuffer

            let toSeq() =
                let head = head
                let count = count
                let contents = contents
                let inline toIndex i = (head + i) % contents.Length
                seq { for i = 0 to count - 1 do contents[toIndex i] }

            new (initialCapacity: int) =
                if initialCapacity < 0 then failwith "capacity is less than 0"
                Queue<'T>(Array.zeroCreate<'T>(initialCapacity), 0)

            new () = Queue<'T>(4)

            new (xs: IEnumerable<'T>) =
                let arr = Array.ofSeq xs
                Queue<'T>(arr, arr.Length)

            member _.Count = count

            member _.Enqueue (value: 'T) =
                if count = size() then
                    ensure(count + 1)
                contents[tail] <- value
                tail <- (tail + 1) % size()
                count <- count + 1

            member _.Dequeue (): 'T =
                if count = 0 then invalidOp "Queue is empty"
                let value = contents[head]
                head <- (head + 1) % size()
                count <- count - 1
                value

            member _.Peek (): 'T =
                if count = 0 then invalidOp "Queue is empty"
                contents[head]

            member this.TryDequeue (result: 'T byref): bool =
                if count = 0 then
                    false
                else
                    result <- this.Dequeue()
                    true

            member this.TryPeek (result: 'T byref): bool =
                if count = 0 then
                    false
                else
                    result <- this.Peek()
                    true

            member _.Contains(x: 'T) =
                let mutable found = false
                let mutable i = 0
                while i < count && not found do
                    if x = contents[toIndex i] then
                        found <- true
                    else
                        i <- i + 1
                found

            member _.Clear() =
                count <- 0
                head <- 0
                tail <- 0
                Array.fill contents 0 (size()) Unchecked.defaultof<_>

            member _.TrimExcess() =
                if float count / float contents.Length > 0.9 then
                    ensure(count)

            member _.ToArray(): 'T[] =
                let res = ResizeArray<_>(count)
                for i = 0 to count - 1 do
                    res.Add(contents[toIndex i])
                res |> asArray

            member _.CopyTo(target: 'T array, start: int) =
                let mutable i = start
                for i = 0 to count - 1 do
                    target[start + i] <- contents[toIndex i]

            interface IEnumerable<'T> with
                member _.GetEnumerator(): IEnumerator<'T> =
                    toSeq().GetEnumerator()

            interface System.Collections.IEnumerable with
                member _.GetEnumerator(): System.Collections.IEnumerator =
                    toSeq().GetEnumerator() :> System.Collections.IEnumerator

module Text =

    type StringBuilder(value: string, capacity: int) =
        let buf = ResizeArray<string>(capacity)
        do if not (System.String.IsNullOrEmpty value) then buf.Add(value)
        new (capacity: int) = StringBuilder("", capacity)
        new (value: string) = StringBuilder(value, 16)
        new () = StringBuilder("", 16)
        member x.Append(s: string) = buf.Add(s); x
        member x.Append(o: bool) = x.Append(string o)
        member x.Append(c: char) = x.Append(string c)
        member x.Append(o: int8) = x.Append(string o)
        member x.Append(o: byte) = x.Append(string o)
        member x.Append(o: int16) = x.Append(string o)
        member x.Append(o: uint16) = x.Append(string o)
        member x.Append(o: int32) = x.Append(string o)
        member x.Append(o: uint32) = x.Append(string o)
        member x.Append(o: int64) = x.Append(string o)
        member x.Append(o: uint64) = x.Append(string o)
        member x.Append(o: float32) = x.Append(string o)
        member x.Append(o: float) = x.Append(string o)
        member x.Append(s: string, index: int, count: int) = x.Append(s.Substring(index, count))
        member x.Append(cs: char[]) = x.Append(System.String(cs))
        member x.Append(sb: StringBuilder) = x.Append(sb.ToString())
        // member x.Append(o: obj) = x.Append(string o)
        // member x.AppendFormat(fmt: string, o: obj) = x.Append(System.String.Format(fmt, o))
        // member x.AppendFormat(provider: System.IFormatProvider, fmt: string, o: obj) = x.Append(System.String.Format(provider, fmt, o))
        member x.AppendLine() = x.Append(System.Environment.NewLine)
        member x.AppendLine(s: string) = x.Append(s).AppendLine()
        member x.Clear() = buf.Clear(); x
        member x.Length = buf |> asArray |> Array.sumBy (fun s -> s.Length)
        override _.ToString() = System.String.Concat(buf |> asArray)
        member x.ToString(index: int, count: int) = x.ToString().Substring(index, count)
