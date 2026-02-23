;Project topic: Echo sound effect
;
;Description:
;The project implements an echo sound effect algorithm for an audio signal
;stored in a WAV file. The algorithm operates on an input buffer containing
;16-bit audio samples. For each sample of the input signal, the output buffer
;stores the sum of the current value and samples from previous
;positions (n samples back), depending on the specified number of bounces.
;
;Each successive bounce has a progressively lower amplitude, scaled by the
;feedback coefficient raised to the power n (feedback^n).
;After summing all components, the echo effect is obtained.
;The program includes safeguards to prevent reading data outside the
;buffer range.
;
;Completion date: 28.01.2026  
;Semester: 5  
;Academic year: 3  
;Author: Wojciech Korga

    PUBLIC DelayASM


    .code
    DelayASM proc
        ; RCX = input pointer (short*)
        ; RDX = sample count (int)
        ; R8  = output pointer (short*)
        ; R9  = delaySamples (int)
        ; [RSP+40] = bounce (0-10) (int)
        ; [RSP+48] = feedback (0–100) (int)

        push rdx

        ; feedback Q15
        mov eax, [rsp+56]     ; 1..100
        imul eax, 32767
        mov r11d, 100
        cdq
        idiv r11d

        ; Copy feedback to xmm14 for vector operation
        movd xmm14, eax
        vpbroadcastw xmm14, xmm14

        pop rdx

        xor rsi, rsi ; zero the array offset pointer

        push r9 ; save r9 on the stack
        shl r9, 1 ; sample = 2 bytes, so shift left by one bit
    
    OuterLoop:

        vmovdqu xmm0, XMMWORD PTR [rcx+rsi] ; load input samples into vector

        mov r14, [RSP+48] ; load bounce, originally +40 but now +48 due to push r9
        cmp r14, 0 ; check if bounce equals zero, if so skip inner loop
        je NoInnerLoop
    
        xor r10, r10 ; zero inner loop counter 

        movaps  xmm15, xmm14 ; copy feedback value to xmm15

        mov r13, r9 ; copy delaySamples for first iteration

    InnerLoop:

        inc r10 ; increment inner loop counter
        
        mov r12, rcx
        add r12, rsi
        sub r12, r13 ; calculate address of delayed sample
    
        vmovdqu xmm3, XMMWORD PTR [r12] ; load delayed samples

        pmulhw xmm3, xmm15 ; multiply by feedback^i in Q15

        vpaddsw xmm0, xmm0, xmm3 ; add to output accumulator with saturation

        pmulhw  xmm15, xmm14 ; raise feedback to i+1 (r10) power
    
        add r13, r9 ; delaySamples * i, replace previous imul with addition
    
        cmp r14, r10 ; compare number of bounces to current inner loop counter
        jne InnerLoop

    NoInnerLoop:  

        vmovdqu XMMWORD PTR [r8+rsi], xmm0 ; store new sample to output array

        add rsi, 16 ; advance array offset pointer by 16 (8 samples * 2 bytes each)
        sub rdx, 8 ; subtract 8 because operating on vectors of 8 samples (8*16bit=128bit)
        jnz OuterLoop

        pop r9 ; restore old r9 value

        ret
    DelayASM endp
    end